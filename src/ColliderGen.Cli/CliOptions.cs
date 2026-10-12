using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace ColliderGen.Cli;

/// <summary>Parsed command line. Exactly one of <see cref="InputPath"/> and <see cref="AllDirectory"/> is set.</summary>
internal sealed record CliOptions(
    string? InputPath,
    string? AllDirectory,
    string? OutputPath,
    bool DebugDraw,
    ColliderOptions Collider)
{
    private sealed record ColliderFlag(string Name, string ValueHint, string Requirement, Func<string, ColliderOptions, ColliderOptions?> TryApply);

    private static readonly ColliderFlag[] ColliderFlags = [
        Number<byte>("--threshold", NumberStyles.Integer, "an integer between 0 and 255", (options, alpha) => options with { AlphaThreshold = alpha }),
        Number<float>("--epsilon", NumberStyles.Float, "a number", (options, epsilon) => options with { Epsilon = epsilon }),
        Number<int>("--max-vertices", NumberStyles.Integer, "an integer", (options, max) => options with { MaxVertices = max }),
        Choice("--origin", (options, origin) => options with { Origin = origin }, ("center", ColliderOrigin.Center), ("top-left", ColliderOrigin.TopLeft)),
        Choice("--fit", (options, fit) => options with { Fit = fit }, ("inner", ColliderFit.Inner), ("outer", ColliderFit.Outer)),
        Number<int>("--frame-width", NumberStyles.Integer, "an integer", (options, frameWidth) => options with { FrameWidth = frameWidth })
    ];

    private static ColliderFlag Number<T>(string name, NumberStyles styles, string requirement, Func<ColliderOptions, T, ColliderOptions> apply)
        where T : INumberBase<T> =>
        new(name, "N", requirement, (text, options) => T.TryParse(text, styles, CultureInfo.InvariantCulture, out var value) ? apply(options, value) : null);

    private static ColliderFlag Choice<T>(string name, Func<ColliderOptions, T, ColliderOptions> apply, params (string Text, T Value)[] choices) =>
        new(name, string.Join('|', choices.Select(choice => choice.Text)), string.Join(" or ", choices.Select(choice => $"'{choice.Text}'")),
            (text, options) => Array.Find(choices, choice => choice.Text == text) is { Text: not null } match ? apply(options, match.Value) : null);

    private static readonly string[] ValueFlags = ["--all", "--output", .. ColliderFlags.Select(flag => flag.Name)];

    internal static readonly string Usage = BuildUsage();

    private static string BuildUsage()
    {
        var colliderFlags = string.Join(" ", ColliderFlags.Select(flag => $"[{flag.Name} {flag.ValueHint}]"));
        return string.Join(Environment.NewLine,
            $"Usage: collidergen <sprite.png> {colliderFlags} [--output path] [--debug]",
            $"       collidergen --all <directory> {colliderFlags} [--debug]");
    }

    /// <summary>Parses <paramref name="args"/> in one pass. On failure, <paramref name="options"/> is null and <paramref name="error"/> says why.</summary>
    public static bool TryParse(string[] args, [NotNullWhen(true)] out CliOptions? options, [NotNullWhen(false)] out string? error)
    {
        options = null;
        if (!TryReadArguments(args, out var values, out var input, out var debug, out error)) {
            return false;
        }

        values.TryGetValue("--all", out var allDirectory);
        values.TryGetValue("--output", out var outputPath);
        if (!TryValidateTargets(input, allDirectory, outputPath, out error)
            || !TryParseCollider(values, out var collider, out error)) {
            return false;
        }

        options = new CliOptions(input, allDirectory, outputPath, debug, collider);
        error = null;
        return true;
    }

    private static bool TryReadArguments(string[] args, out Dictionary<string, string> values, out string? input, out bool debug, [NotNullWhen(false)] out string? error)
    {
        values = [];
        input = null;
        debug = false;

        for (var i = 0; i < args.Length; i++) {
            var arg = args[i];
            if (arg == "--debug") {
                debug = true;
            } else if (ValueFlags.Contains(arg)) {
                if (!TryReadValueFlag(args, ref i, values, out error)) {
                    return false;
                }
            } else if (arg.StartsWith("--", StringComparison.Ordinal)) {
                error = $"Unknown option: {arg}";
                return false;
            } else if (input is null) {
                input = arg;
            } else {
                error = $"Unexpected argument: {arg}";
                return false;
            }
        }

        error = null;
        return true;
    }

    private static bool TryReadValueFlag(string[] args, ref int index, Dictionary<string, string> values, [NotNullWhen(false)] out string? error)
    {
        var flag = args[index];
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal)) {
            error = $"{flag} requires a value.";
            return false;
        }

        if (!values.TryAdd(flag, args[++index])) {
            error = $"{flag} was specified more than once.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryValidateTargets(string? input, string? allDirectory, string? outputPath, [NotNullWhen(false)] out string? error)
    {
        error = (allDirectory is null == input is null, allDirectory is not null && outputPath is not null) switch {
            (true, _) => "Specify either a sprite path or --all <directory>.",
            (_, true) => "--output cannot be used with --all.",
            _ => null
        };
        return error is null;
    }

    private static bool TryParseCollider(Dictionary<string, string> values, out ColliderOptions collider, [NotNullWhen(false)] out string? error)
    {
        collider = new ColliderOptions();
        error = null;

        foreach (var flag in ColliderFlags) {
            if (!values.TryGetValue(flag.Name, out var text)) {
                continue;
            }

            if (flag.TryApply(text, collider) is not { } updated) {
                error = $"{flag.Name} must be {flag.Requirement}, got '{text}'.";
                return false;
            }

            collider = updated;
            if (collider.TryValidate(out var validationError)) {
                continue;
            }

            error = $"{flag.Name}: {validationError}";
            return false;
        }

        return true;
    }
}
