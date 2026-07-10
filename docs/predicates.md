# Predicate dialect

The **canonical specification** of the VION presentation-predicate dialect — the small,
typed expression language carried in `Presentation.VisibleWhen` (the `visibleWhen` field of
a property's presentation document). It is a typed **subset of the dashboard's existing
widget-expression language** (a [jsep](https://github.com/EricSmekens/jsep)-based dialect),
so authors write one syntax across custom widgets and visibility predicates.

This document and the sibling [`Vion.Contracts/Predicates/predicate-conformance.json`](../Vion.Contracts/Predicates/predicate-conformance.json)
vector are the single source of truth. dale-sdk RFC 0016 (`[ExistsWhen]`, config-time
structural gating) reuses this exact grammar, semantics, and vector.

> **`Vion.Contracts` transports the predicate; it never evaluates it.** There is no C#
> evaluator in this package. The evaluators live downstream and all conform to the vector:
> the dashboard's jsep-subset compiler (parse + eval), the dale-sdk DevHost plain-JS
> evaluator (eval), and the dale-sdk analyzer's recursive-descent parser (parse and
> type-check only — it never evaluates). RFC 0016 adds the first C# runtime evaluator later,
> against the same vector.

## Grammar (v1)

```
predicate   := orExpr
orExpr      := andExpr ( "||" andExpr )*
andExpr     := unaryExpr ( "&&" unaryExpr )*
unaryExpr   := "!" negand | "(" predicate ")" | comparison | membership | boolRef
negand      := boolRef | "(" predicate ")"   // NOT a bare comparison: in JS, !A == 5
                                             // parses as (!A) == 5 — parenthesize instead
comparison  := ref ( "==" | "!=" | "<" | "<=" | ">" | ">=" ) literal
membership  := ref "in" "[" literal ( "," literal )* "]"
ref         := identifier | identifier "." identifier      // Property | Service.Property
boolRef     := ref                                          // must type-check to bool
literal     := integer | "true" | "false" | string
string      := "'" chars "'" | '"' chars '"'   // both quote styles; single quotes are the
                                               // documented authoring style (C# attributes)
```

- **Valid jsep / JS.** Every predicate parses with the dashboard's existing pipeline
  (`in` needs `jsep.addBinaryOp('in')`; array literals are native jsep `ArrayExpression`).
- **Valid-looking C#.** Block authors read and write it natively, and `nameof()`
  concatenation works in the attribute:
  `[Presentation(VisibleWhen = nameof(DirectMeasurement) + " == false")]`.
- **Enum members are quoted strings** (`Mode == 'Eco'`), matching the wire representation —
  enum values travel as member-name strings, and `statusMappings` / `enumLabels` key on
  them. Unquoted identifiers on the right of a comparison would be indistinguishable from
  property references under JS evaluation, so they are rejected (see below).
- **Refs sit on the left** of every comparison (no Yoda conditions), have **at most two
  segments**, and are **case-sensitive**.
- **String literals accept either quote style** — `'Eco'` and `"Eco"` parse identically.
  jsep's AST does not record which quote was used, so single-quote-only is unenforceable in
  the TypeScript subset validator; both are therefore accepted across all consumers.
  **Single quotes are the recommended authoring style**, because they need no escaping inside
  a C# attribute string (`VisibleWhen = "Mode == 'Eco'"`).
- **Integer literals are int32-range** — exact in every evaluator (JS doubles are exact to
  2^53, C# ints are int32); larger magnitudes have no use in a visibility gate.

### Reference scope (addressing)

- **Bare ref** (`DirectMeasurement`) — a property on the *same service* as the annotated
  member.
- **Qualified ref** (`ChargingPoint2.EnableCharging`) — a property on a *sibling service of
  the same logic-block instance*. The root service is addressed the same way, by its
  class-name identifier (`ChargingStationMultiPointSimulation.IsExternallyLocked`); it is
  not special on the wire.

Referenced targets must be `bool`, `enum`, integer, or `string` service properties. The
full identifier-formation rules (definition-level identifiers vs runtime GUIDs, the
name-collision resolution rule) are enforced by the dale-sdk analyzer and documented in the
SDK docs; they are out of scope for this grammar reference.

### Not in the dialect

The analyzer and the dashboard subset-validator both **reject**: arithmetic (`+ - * / %`,
unary `-`), the ternary `?:`, function calls, `|`-pipes, computed member access (`a[b]`),
refs deeper than two segments, and string escapes beyond `\'`.

## Evaluation semantics

### Type discipline (checked at compile time by the SDK analyzer)

- `==` / `!=` — for `bool` / `enum` / `string` / integer refs against a **matching-typed**
  literal.
- `<` / `<=` / `>` / `>=` — for **integer** refs only.
- `in` — for `enum` / `string` / integer refs against a **homogeneous** list of literals.
- `&&` / `||` / `!` — over boolean-shaped operands.
- A **bare ref** must be `bool`.

Excluded reference types: `double` / `float` (analog values flap), `WriteOnly` properties
(the UI only ever sees the `"***"` sentinel), measuring-point-only members (no retained
property state in the UI store), struct fields, arrays, and cross-block / cross-gateway
refs.

### Fail-open evaluation (at render time, per property)

Visibility is computed reactively in the UI. The evaluator never throws a property off the
screen because a predicate is broken — it **fails open** (shows the member):

1. no `visibleWhen` → **visible**;
2. parse failure or an out-of-subset node → **visible**, plus one dev-console warning;
3. a ref that does not resolve to a known sibling property → **visible**, plus one warning;
4. any referenced value is `undefined` (no retained message has arrived yet) → **visible**,
   no warning — this is transient, and the platform's 3-state value invariant makes
   `undefined` distinguishable from `null`;
5. otherwise **evaluate**, and truthiness-test the final result. Explicit `null` is a real
   value and participates: `X == 5` with `X = null` → hidden; `!X` with `X = null` →
   visible.

### Loose equality is the reference behavior

The reference evaluator (the dashboard compiler) uses JavaScript `==`. This is harmless
because the analyzer guarantees both operands are the same type; the only observable effect
is around `null`, which the conformance vector pins explicitly. Consumers written in other
languages must reproduce the vector's results, loose-equality `null` cases included.

### Two evaluation profiles, one dialect

The fail-open ladder and the `null`-participates rule above are the **UI profile** — the one
this release ships. Live UI values can legitimately be absent, so the UI evaluators
(dashboard, DevHost) fail open and treat `null` as a real, participating value.

A future **strict profile** (dale-sdk RFC 0016's `[ExistsWhen]` C# evaluator) shares this
exact grammar but never faces an absent value: its references are present and typed by
construction, a missing/`null` reference is a hard configuration error (**fail-closed** — an
existence gate must be deterministic), and the result is a real boolean with no truthiness
coercion. Both profiles agree on every same-typed, non-null comparison, which is where C#
`==` and JS `==` coincide exactly.

The conformance vector encodes the split: eval cases that exercise `null`/`undefined` carry
`"profile": "ui"`; **untagged eval cases are core** and bind *every* evaluator, including the
future strict C# one.

## Conformance vector

[`Vion.Contracts/Predicates/predicate-conformance.json`](../Vion.Contracts/Predicates/predicate-conformance.json)
is the cross-implementation drift guard. It holds two case lists:

- **`eval`** — `{ name, predicate, values, expected, profile? }`. `values` is keyed by ref
  string; each consumer maps it onto its own context shape. `expected` is the boolean the
  predicate evaluates to (which the consumer then truthiness-tests into visible / hidden). A
  case that exercises `null` / `undefined` carries `"profile": "ui"` and binds only the
  UI-profile evaluators (dashboard, DevHost); an untagged case is **core** and binds every
  evaluator, including RFC 0016's future C# one.
- **`parse`** — `{ name, predicate, valid, reason? }`. Asserts whether a string is inside
  the grammar subset above. Every consumer runs these, including the parse-only SDK
  analyzer.

The file is plain JSON (no comments), so `JSON.parse` and `System.Text.Json` both read it
directly. Consumers **vendor** a copy with a provenance header (source repo + SHA); the
vector is near-write-once, and manual sync is an accepted residual risk.
