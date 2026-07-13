# Predicate dialect

The **canonical specification** of the VION presentation-predicate dialect — the small,
typed expression language carried in `Presentation.VisibleWhen` (the `visibleWhen` field of
a property's presentation document). It is a typed **subset of the dashboard's existing
widget-expression language** (a [jsep](https://github.com/EricSmekens/jsep)-based dialect),
so authors write one syntax across custom widgets and visibility predicates.

This document and the sibling [`Vion.Contracts/Predicates/predicate-conformance.json`](../Vion.Contracts/Predicates/predicate-conformance.json)
vector are the single source of truth. dale-sdk RFC 0016 (`[IncludedWhen]`, config-time
structural gating) reuses this exact grammar, semantics, and vector.

> **`Vion.Contracts` transports the predicate — and, since RFC 0016, evaluates it under the
> strict profile.** [`Vion.Contracts/Predicates/Predicate.cs`](../Vion.Contracts/Predicates/Predicate.cs)
> is a reflection-free/AOT-safe parser + strict evaluator (`Predicate.Parse(string)` →
> `Evaluate(IReadOnlyDictionary<string, JsonNode?>)`), consumed by the dale-sdk Live-mode
> binders and cloud-api's activation/projection to resolve config-time inclusion gates. It
> joins the other conformant implementations: the dashboard's jsep-subset compiler (parse +
> eval, UI profile), the dale-sdk DevHost plain-JS evaluator (eval, UI profile), and the
> dale-sdk analyzer's recursive-descent parser (parse + type-check only — the Generators
> assembly is netstandard2.0 and cannot reference this package, so it stays a second, vector-
> pinned C# parser). This supersedes the earlier "this package never evaluates" posture
> (deliberate, per the RFC 0016 spec §3).

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

The fail-open ladder and the `null`-participates rule above are the **UI profile**. Live UI
values can legitimately be absent, so the UI evaluators (dashboard, DevHost) fail open and
treat `null` as a real, participating value.

The **strict profile** (RFC 0016's `[IncludedWhen]` inclusion gate, implemented by
[`Predicate`](../Vion.Contracts/Predicates/Predicate.cs)) shares this exact grammar but never
faces an absent value: its references are present and typed by construction, a missing/`null`
reference is a hard configuration error (**fail-closed** — an inclusion gate must be
deterministic), and the result is a real boolean with no truthiness coercion. Both profiles
agree on every same-typed, non-null comparison, which is where C# `==` and JS `==` coincide
exactly.

The conformance vector encodes the split: eval cases that exercise `null`/`undefined` carry
`"profile": "ui"` (or `"strict"` for the fail-closed counterpart); **untagged eval cases are
core** and bind *every* evaluator, the strict C# one included.

#### The strict profile (RFC 0016 inclusion gates)

The strict evaluator resolves whether a gated member is part of a configured instance. It
narrows the dialect in exactly one place — **reference scope** — and hardens the failure
mode; nothing else about the grammar or type discipline changes.

- **Reference scope.** In an `[IncludedWhen]` gate a reference is a **bare, single-segment
  ref to an `[InstantiationParameter]` scalar of the same block** — the operator-chosen,
  config-time properties (`bool` / `enum` / integer / `string`, the discrete-scalar set the
  [Type discipline](#type-discipline-checked-at-compile-time-by-the-sdk-analyzer) section
  allows). This is *narrower* than the
  UI profile's addressing (which permits qualified `Service.Property` refs to any sibling
  service property). The dale-sdk analyzer (DALE043) rejects a qualified ref or a ref that
  does not resolve to an `[InstantiationParameter]`, so a strict-profile evaluator never has
  to police scope — it evaluates whatever context it is handed. The evaluator itself imposes
  no scope rule: its context is a flat map keyed by each ref's full text (a bare `Property`,
  or a two-segment `Service.Property` for the shared core cases), so the same evaluator also
  serves the broader keys the core vector uses.
- **Context shape.** The context is `IReadOnlyDictionary<string, JsonNode?>` keyed by ref
  text; each value is a JSON scalar in the wire form — an **enum as its member-name string**,
  an **integer as a JSON number**, plus `bool` and `string` — produced by
  `PropertyValueCodec.ClrToJson`. (Constructing the context with raw `(int)` casts or
  `ToString()` instead of the codec is the classic context bug; it lives in construction, not
  in the evaluator, so the shared vector cannot catch it — cover it with a consumer-side
  test.)
- **Fail-closed rules.** `Evaluate` throws `PredicateEvaluationException` when a referenced
  value is **missing** (absent key), **`null`**, or of a **JSON kind that does not match the
  compared literal** (e.g. an integer gate over a string value). An unparseable predicate
  throws `PredicateSyntaxException` from `Parse`. A consumer treats either as a hard
  configuration error — skip-the-member / fail-activation — never as a silently "included"
  member.

## Conformance vector

[`Vion.Contracts/Predicates/predicate-conformance.json`](../Vion.Contracts/Predicates/predicate-conformance.json)
is the cross-implementation drift guard. It holds two case lists:

- **`eval`** — `{ name, predicate, values, expected, profile?, error? }`. `values` is keyed
  by ref string; each consumer maps it onto its own context shape. Outcome is one of:
  - **`expected`** — the boolean the predicate evaluates to (which a UI consumer then
    truthiness-tests into visible / hidden, and a strict consumer takes as the resolved gate).
  - **`"error": true`** — a **strict-profile fail-closed** case: the strict evaluator must
    throw rather than return a boolean (missing ref, `null` value, or type mismatch). It
    carries no `expected` and always `"profile": "strict"`. UI evaluators do not run it.

  Profiles: a case that exercises `null` / `undefined` carries `"profile": "ui"` (fail-open,
  binds the dashboard + DevHost) or `"profile": "strict"` (fail-closed, binds
  [`Predicate`](../Vion.Contracts/Predicates/Predicate.cs)); a `"profile": "strict"` positive
  case is one whose parameter-shaped values only the strict evaluator is asked to run. An
  **untagged** case is **core** and binds *every* evaluator, the strict C# one included.
- **`parse`** — `{ name, predicate, valid, reason? }`. Asserts whether a string is inside
  the grammar subset above. Every consumer runs these, including the parse-only SDK analyzer
  and this package's own `Predicate.TryParse`.

The file is plain JSON (no comments), so `JSON.parse` and `System.Text.Json` both read it
directly. Consumers **vendor** a copy with a provenance header (source repo + SHA); the
vector is near-write-once, and manual sync is an accepted residual risk.
