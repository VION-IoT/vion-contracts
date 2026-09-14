using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace Vion.Contracts.Predicates
{
    /// <summary>
    ///     A parsed predicate in the shared VION dialect (<c>docs/predicates.md</c>), evaluable under the
    ///     <b>strict profile</b>: references are present and typed by construction, and a missing/<c>null</c>
    ///     reference or a type mismatch is a hard, fail-closed error rather than a coerced truthiness result.
    ///     This is the config-time inclusion-gate evaluator RFC 0016 introduces — the first evaluator this
    ///     package ships (it previously only transported predicates).
    /// </summary>
    /// <remarks>
    ///     Reflection-free and AOT-safe (the <c>WriteOnlyCodec</c> precedent): the parser is a self-contained
    ///     recursive-descent over the grammar, and evaluation reads only <see cref="JsonValue" /> scalars via
    ///     the strongly-typed <c>TryGetValue&lt;bool/long/string&gt;</c> path. The evaluation context is keyed
    ///     by the reference's full text (a bare <c>Property</c> or a two-segment <c>Service.Property</c>),
    ///     exactly as the conformance vector's <c>values</c> object is keyed; each value is a JSON scalar —
    ///     enum member-name string, integer number, bool, or string — matching
    ///     <see cref="Vion.Contracts.Codec.PropertyValueCodec.ClrToJson" />. Behavior is pinned by
    ///     <c>Predicates/predicate-conformance.json</c>; the grammar is a fresh implementation of the same
    ///     dialect the dale-sdk analyzer parses.
    /// </remarks>
    public sealed class Predicate
    {
        private readonly Node _root;

        private readonly string _text;

        private Predicate(Node root, string text)
        {
            _root = root;
            _text = text;
        }

        /// <summary>
        ///     Parses <paramref name="text" /> into an evaluable predicate, throwing
        ///     <see cref="PredicateSyntaxException" /> when it is outside the dialect grammar (fail-closed).
        /// </summary>
        public static Predicate Parse(string text)
        {
            if (!TryParse(text, out var predicate, out var error))
            {
                throw new PredicateSyntaxException(error!);
            }

            return predicate!;
        }

        /// <summary>
        ///     Attempts to parse <paramref name="text" />. Returns <c>true</c> and sets
        ///     <paramref name="predicate" /> on success; returns <c>false</c> and sets <paramref name="error" />
        ///     (a human-readable reason) when the string is outside the grammar. Never throws — the non-throwing
        ///     shape a producer-side check (or the conformance vector's parse cases) can dispatch on.
        /// </summary>
        public static bool TryParse(string text, out Predicate? predicate, out string? error)
        {
            if (!Tokenizer.TryTokenize(text, out var tokens, out error))
            {
                predicate = null;
                return false;
            }

            var root = new Parser(tokens).TryParse(out error);
            if (root is null)
            {
                predicate = null;
                return false;
            }

            predicate = new Predicate(root, text);
            error = null;
            return true;
        }

        /// <summary>
        ///     Evaluates the predicate against <paramref name="values" /> (reference text → JSON scalar),
        ///     returning the boolean result. Throws <see cref="PredicateEvaluationException" /> when a referenced
        ///     value is missing or <c>null</c>, or when its JSON kind does not match the compared literal
        ///     (fail-closed).
        /// </summary>
        public bool Evaluate(IReadOnlyDictionary<string, JsonNode?> values)
        {
            return _root.Eval(values);
        }

        /// <summary>The original predicate string this instance was parsed from.</summary>
        public override string ToString()
        {
            return _text;
        }

        // ── Value access (strict / fail-closed) ──

        private static JsonNode RequireValue(string reference, IReadOnlyDictionary<string, JsonNode?> values)
        {
            if (!values.TryGetValue(reference, out var node))
            {
                throw new
                    PredicateEvaluationException($"reference '{reference}' is not present in the evaluation context (strict profile: every reference must resolve to a value)");
            }

            if (node is null)
            {
                throw new
                    PredicateEvaluationException($"reference '{reference}' is null (strict profile: an inclusion gate must be deterministic, so a null value is a hard error)");
            }

            return node;
        }

        private static bool RequireBool(string reference, IReadOnlyDictionary<string, JsonNode?> values)
        {
            if (RequireValue(reference, values) is JsonValue value && value.TryGetValue<bool>(out var result))
            {
                return result;
            }

            throw new PredicateEvaluationException($"reference '{reference}' is not a boolean value");
        }

        private static long RequireLong(string reference, IReadOnlyDictionary<string, JsonNode?> values)
        {
            if (RequireValue(reference, values) is JsonValue value && TryGetInteger(value, out var result))
            {
                return result;
            }

            throw new PredicateEvaluationException($"reference '{reference}' is not an integer value");
        }

        /// <summary>
        ///     Reads an integer from a JSON scalar irrespective of its CLR backing — a parsed JSON number and a
        ///     <see cref="Vion.Contracts.Codec.PropertyValueCodec.ClrToJson" /> <c>long</c> both satisfy
        ///     <c>TryGetValue&lt;long&gt;</c>, while a hand-built <c>JsonValue.Create(3)</c> is <c>int</c>-backed
        ///     — so the "integer as a number" contract holds regardless of how a consumer builds the context.
        /// </summary>
        private static bool TryGetInteger(JsonValue value, out long result)
        {
            if (value.TryGetValue(out result))
            {
                return true;
            }

            if (value.TryGetValue<int>(out var narrow))
            {
                result = narrow;
                return true;
            }

            result = 0;
            return false;
        }

        private static string RequireString(string reference, IReadOnlyDictionary<string, JsonNode?> values)
        {
            if (RequireValue(reference, values) is JsonValue value && value.TryGetValue<string>(out var result))
            {
                return result;
            }

            throw new PredicateEvaluationException($"reference '{reference}' is not a string value");
        }

        // ── AST ──

        private abstract class Node
        {
            public abstract bool Eval(IReadOnlyDictionary<string, JsonNode?> values);
        }

        private sealed class OrNode : Node
        {
            private readonly Node _left;

            private readonly Node _right;

            public OrNode(Node left, Node right)
            {
                _left = left;
                _right = right;
            }

            public override bool Eval(IReadOnlyDictionary<string, JsonNode?> values)
            {
                return _left.Eval(values) || _right.Eval(values);
            }
        }

        private sealed class AndNode : Node
        {
            private readonly Node _left;

            private readonly Node _right;

            public AndNode(Node left, Node right)
            {
                _left = left;
                _right = right;
            }

            public override bool Eval(IReadOnlyDictionary<string, JsonNode?> values)
            {
                return _left.Eval(values) && _right.Eval(values);
            }
        }

        private sealed class NotNode : Node
        {
            private readonly Node _operand;

            public NotNode(Node operand)
            {
                _operand = operand;
            }

            public override bool Eval(IReadOnlyDictionary<string, JsonNode?> values)
            {
                return !_operand.Eval(values);
            }
        }

        private sealed class BoolRefNode : Node
        {
            private readonly string _reference;

            public BoolRefNode(string reference)
            {
                _reference = reference;
            }

            public override bool Eval(IReadOnlyDictionary<string, JsonNode?> values)
            {
                return RequireBool(_reference, values);
            }
        }

        private sealed class ComparisonNode : Node
        {
            private readonly Literal _literal;

            private readonly string _operator;

            private readonly string _reference;

            public ComparisonNode(string reference, string op, Literal literal)
            {
                _reference = reference;
                _operator = op;
                _literal = literal;
            }

            public override bool Eval(IReadOnlyDictionary<string, JsonNode?> values)
            {
                switch (_operator)
                {
                    case "<":
                    case "<=":
                    case ">":
                    case ">=":
                        if (_literal.Kind != LiteralKind.Integer)
                        {
                            throw new PredicateEvaluationException($"relational operator '{_operator}' requires an integer literal (reference '{_reference}')");
                        }

                        var number = RequireLong(_reference, values);
                        return _operator switch
                        {
                            "<" => number < _literal.IntValue,
                            "<=" => number <= _literal.IntValue,
                            ">" => number > _literal.IntValue,
                            _ => number >= _literal.IntValue,
                        };

                    default:
                        var equal = _literal.Kind switch
                        {
                            LiteralKind.Integer => RequireLong(_reference, values) == _literal.IntValue,
                            LiteralKind.Boolean => RequireBool(_reference, values) == _literal.BoolValue,
                            _ => string.Equals(RequireString(_reference, values), _literal.StringValue, StringComparison.Ordinal),
                        };
                        return _operator == "==" ? equal : !equal;
                }
            }
        }

        private sealed class MembershipNode : Node
        {
            private readonly IReadOnlyList<Literal> _items;

            private readonly string _reference;

            public MembershipNode(string reference, IReadOnlyList<Literal> items)
            {
                _reference = reference;
                _items = items;
            }

            public override bool Eval(IReadOnlyDictionary<string, JsonNode?> values)
            {
                var node = RequireValue(_reference, values);

                if (node is JsonValue value)
                {
                    if (TryGetInteger(value, out var number))
                    {
                        return ContainsInteger(number);
                    }

                    if (value.TryGetValue<string>(out var text))
                    {
                        return ContainsString(text);
                    }
                }

                throw new PredicateEvaluationException($"reference '{_reference}' is not an integer or string value; 'in' membership requires one");
            }

            private bool ContainsInteger(long number)
            {
                foreach (var item in _items)
                {
                    if (item.Kind == LiteralKind.Integer && item.IntValue == number)
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool ContainsString(string text)
            {
                foreach (var item in _items)
                {
                    if (item.Kind == LiteralKind.String && string.Equals(item.StringValue, text, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private enum LiteralKind
        {
            Integer,

            Boolean,

            String,
        }

        private readonly struct Literal
        {
            private Literal(LiteralKind kind, int intValue, bool boolValue, string stringValue)
            {
                Kind = kind;
                IntValue = intValue;
                BoolValue = boolValue;
                StringValue = stringValue;
            }

            public LiteralKind Kind { get; }

            public int IntValue { get; }

            public bool BoolValue { get; }

            public string StringValue { get; }

            public static Literal Integer(int value)
            {
                return new Literal(LiteralKind.Integer, value, false, string.Empty);
            }

            public static Literal Boolean(bool value)
            {
                return new Literal(LiteralKind.Boolean, 0, value, string.Empty);
            }

            public static Literal String(string value)
            {
                return new Literal(LiteralKind.String, 0, false, value);
            }
        }

        // ── Recursive-descent parser ──

        private sealed class Parser
        {
            private readonly IReadOnlyList<Token> _tokens;

            private int _position;

            private Token Current
            {
                get => _tokens[_position];
            }

            public Parser(IReadOnlyList<Token> tokens)
            {
                _tokens = tokens;
            }

            /// <summary>Parses the full token stream, or returns <c>null</c> with a reason in <paramref name="error" />.</summary>
            public Node? TryParse(out string? error)
            {
                Node node;
                try
                {
                    node = ParseOr();
                    if (Current.Kind != TokenKind.End)
                    {
                        error = $"unexpected '{Current.Text}' after a complete predicate";
                        return null;
                    }
                }
                catch (ParseException ex)
                {
                    error = ex.Message;
                    return null;
                }

                error = null;
                return node;
            }

            private Node ParseOr()
            {
                var left = ParseAnd();
                while (Current.Kind == TokenKind.PipePipe)
                {
                    _position++;
                    left = new OrNode(left, ParseAnd());
                }

                return left;
            }

            private Node ParseAnd()
            {
                var left = ParseUnary();
                while (Current.Kind == TokenKind.AmpAmp)
                {
                    _position++;
                    left = new AndNode(left, ParseUnary());
                }

                return left;
            }

            private Node ParseUnary()
            {
                if (Current.Kind == TokenKind.Bang)
                {
                    _position++;
                    return new NotNode(ParseNegand());
                }

                if (Current.Kind == TokenKind.LParen)
                {
                    return ParseParenthesized();
                }

                return ParseComparisonOrRef();
            }

            // negand := boolRef | "(" predicate ")" — deliberately NOT a comparison, so "!A == 5" fails.
            private Node ParseNegand()
            {
                if (Current.Kind == TokenKind.LParen)
                {
                    return ParseParenthesized();
                }

                return new BoolRefNode(ParseReference());
            }

            private Node ParseParenthesized()
            {
                Expect(TokenKind.LParen, "(");
                var inner = ParseOr();
                Expect(TokenKind.RParen, ")");
                return inner;
            }

            private Node ParseComparisonOrRef()
            {
                var reference = ParseReference();

                switch (Current.Kind)
                {
                    case TokenKind.EqEq:
                    case TokenKind.NotEq:
                    case TokenKind.Lt:
                    case TokenKind.Le:
                    case TokenKind.Gt:
                    case TokenKind.Ge:
                        var op = Current.Text;
                        _position++;
                        return new ComparisonNode(reference, op, ParseLiteral());

                    case TokenKind.Ident when Current.Text == "in":
                        _position++;
                        return ParseMembership(reference);

                    default:
                        return new BoolRefNode(reference);
                }
            }

            private Node ParseMembership(string reference)
            {
                Expect(TokenKind.LBracket, "[");
                var items = new List<Literal> { ParseLiteral() };
                while (Current.Kind == TokenKind.Comma)
                {
                    _position++;
                    items.Add(ParseLiteral());
                }

                Expect(TokenKind.RBracket, "]");
                return new MembershipNode(reference, items);
            }

            private string ParseReference()
            {
                var first = ExpectIdentifier();
                if (Current.Kind != TokenKind.Dot)
                {
                    return first;
                }

                _position++;
                var second = ExpectIdentifier();
                if (Current.Kind == TokenKind.Dot)
                {
                    throw new ParseException("references may have at most two segments (Property or Service.Property)");
                }

                return first + "." + second;
            }

            private Literal ParseLiteral()
            {
                switch (Current.Kind)
                {
                    case TokenKind.Int:
                        var integer = Current.IntValue;
                        _position++;
                        return Literal.Integer(integer);

                    case TokenKind.Str:
                        var text = Current.Text;
                        _position++;
                        return Literal.String(text);

                    case TokenKind.Ident when Current.Text == "true" || Current.Text == "false":
                        var flag = Current.Text == "true";
                        _position++;
                        return Literal.Boolean(flag);

                    case TokenKind.Ident:
                        // A bare identifier where a literal is required — the classic "unquoted enum member"
                        // (Mode == Eco): the right side of a comparison must be a quoted literal.
                        throw new ParseException($"'{Current.Text}' is not a literal — the right side of a comparison must be a literal (quote enum/string values, e.g. 'Eco')");

                    default:
                        throw new ParseException("expected a literal (integer, true/false, or a quoted string)");
                }
            }

            private string ExpectIdentifier()
            {
                if (Current.Kind != TokenKind.Ident)
                {
                    throw new ParseException($"expected an identifier but found '{Current.Text}' (references sit on the left of a comparison)");
                }

                // true/false are literals, not references — reject them in reference position so "false == X"
                // fails as a Yoda comparison, matching the dashboard's jsep.
                if (Current.Text == "true" || Current.Text == "false")
                {
                    throw new ParseException($"'{Current.Text}' is a literal, not a reference — a reference must be on the left of a comparison");
                }

                var identifier = Current.Text;
                _position++;
                return identifier;
            }

            private void Expect(TokenKind kind, string display)
            {
                if (Current.Kind != kind)
                {
                    throw new ParseException($"expected '{display}' but found '{Current.Text}'");
                }

                _position++;
            }
        }

        private sealed class ParseException : Exception
        {
            public ParseException(string message) : base(message)
            {
            }
        }

        // ── Tokenizer ──

        private enum TokenKind
        {
            Ident,

            Int,

            Str,

            Dot,

            LParen,

            RParen,

            LBracket,

            RBracket,

            Comma,

            Bang,

            EqEq,

            NotEq,

            Lt,

            Le,

            Gt,

            Ge,

            AmpAmp,

            PipePipe,

            End,
        }

        private readonly struct Token
        {
            public Token(TokenKind kind, string text, int intValue = 0)
            {
                Kind = kind;
                Text = text;
                IntValue = intValue;
            }

            public TokenKind Kind { get; }

            public string Text { get; }

            public int IntValue { get; }
        }

        private static class Tokenizer
        {
            public static bool TryTokenize(string? text, out IReadOnlyList<Token> tokens, out string? error)
            {
                var list = new List<Token>();
                if (string.IsNullOrWhiteSpace(text))
                {
                    tokens = list;
                    error = "predicate is empty";
                    return false;
                }

                var source = text!;
                var index = 0;
                while (index < source.Length)
                {
                    var c = source[index];

                    if (char.IsWhiteSpace(c))
                    {
                        index++;
                        continue;
                    }

                    if (char.IsLetter(c) || c == '_')
                    {
                        var start = index;
                        while (index < source.Length && (char.IsLetterOrDigit(source[index]) || source[index] == '_'))
                        {
                            index++;
                        }

                        list.Add(new Token(TokenKind.Ident, source.Substring(start, index - start)));
                        continue;
                    }

                    if (char.IsDigit(c))
                    {
                        var start = index;
                        while (index < source.Length && char.IsDigit(source[index]))
                        {
                            index++;
                        }

                        var digits = source.Substring(start, index - start);
                        if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
                        {
                            tokens = list;
                            error = $"integer literal '{digits}' is out of the supported int32 range";
                            return false;
                        }

                        list.Add(new Token(TokenKind.Int, digits, value));
                        continue;
                    }

                    if (c == '\'' || c == '"')
                    {
                        if (!TryReadString(source, ref index, c, out var value, out error))
                        {
                            tokens = list;
                            return false;
                        }

                        list.Add(new Token(TokenKind.Str, value!));
                        continue;
                    }

                    switch (c)
                    {
                        case '.':
                            list.Add(new Token(TokenKind.Dot, "."));
                            index++;
                            continue;
                        case '(':
                            list.Add(new Token(TokenKind.LParen, "("));
                            index++;
                            continue;
                        case ')':
                            list.Add(new Token(TokenKind.RParen, ")"));
                            index++;
                            continue;
                        case '[':
                            list.Add(new Token(TokenKind.LBracket, "["));
                            index++;
                            continue;
                        case ']':
                            list.Add(new Token(TokenKind.RBracket, "]"));
                            index++;
                            continue;
                        case ',':
                            list.Add(new Token(TokenKind.Comma, ","));
                            index++;
                            continue;
                        case '!':
                            if (Peek(source, index + 1) == '=')
                            {
                                list.Add(new Token(TokenKind.NotEq, "!="));
                                index += 2;
                            }
                            else
                            {
                                list.Add(new Token(TokenKind.Bang, "!"));
                                index++;
                            }

                            continue;
                        case '=':
                            if (Peek(source, index + 1) == '=')
                            {
                                list.Add(new Token(TokenKind.EqEq, "=="));
                                index += 2;
                                continue;
                            }

                            tokens = list;
                            error = "assignment '=' is not valid; use '==' for equality";
                            return false;
                        case '<':
                            if (Peek(source, index + 1) == '=')
                            {
                                list.Add(new Token(TokenKind.Le, "<="));
                                index += 2;
                            }
                            else
                            {
                                list.Add(new Token(TokenKind.Lt, "<"));
                                index++;
                            }

                            continue;
                        case '>':
                            if (Peek(source, index + 1) == '=')
                            {
                                list.Add(new Token(TokenKind.Ge, ">="));
                                index += 2;
                            }
                            else
                            {
                                list.Add(new Token(TokenKind.Gt, ">"));
                                index++;
                            }

                            continue;
                        case '&':
                            if (Peek(source, index + 1) == '&')
                            {
                                list.Add(new Token(TokenKind.AmpAmp, "&&"));
                                index += 2;
                                continue;
                            }

                            tokens = list;
                            error = "'&' is not valid; use '&&' for logical AND";
                            return false;
                        case '|':
                            if (Peek(source, index + 1) == '|')
                            {
                                list.Add(new Token(TokenKind.PipePipe, "||"));
                                index += 2;
                                continue;
                            }

                            tokens = list;
                            error = "pipe '|' is not in the dialect; use '||' for logical OR";
                            return false;
                        default:
                            tokens = list;
                            error = $"unexpected character '{c}' (arithmetic, ternary, and function calls are not in the dialect)";
                            return false;
                    }
                }

                list.Add(new Token(TokenKind.End, "<end>"));
                tokens = list;
                error = null;
                return true;
            }

            private static bool TryReadString(string text, ref int index, char quote, out string? value, out string? error)
            {
                var builder = new StringBuilder();
                index++; // consume the opening quote
                while (index < text.Length)
                {
                    var c = text[index];
                    if (c == '\\')
                    {
                        if (Peek(text, index + 1) == quote)
                        {
                            builder.Append(quote);
                            index += 2;
                            continue;
                        }

                        value = null;
                        error = "string escapes beyond \\' (or \\\") are not in the dialect";
                        return false;
                    }

                    if (c == quote)
                    {
                        index++; // consume the closing quote
                        value = builder.ToString();
                        error = null;
                        return true;
                    }

                    builder.Append(c);
                    index++;
                }

                value = null;
                error = "unterminated string literal";
                return false;
            }

            private static char Peek(string text, int index)
            {
                return index < text.Length ? text[index] : '\0';
            }
        }
    }
}
