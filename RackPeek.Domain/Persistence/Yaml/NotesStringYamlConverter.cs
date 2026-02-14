using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

public sealed class NotesStringYamlConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(string);
    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        return scalar.Value;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is null)
        {
            emitter.Emit(new Scalar(
                AnchorName.Empty,
                TagName.Empty,
                "",
                ScalarStyle.Plain,
                true,
                true));
            return;
        }

        var s = (string)value;

        if (s.Contains('\n'))
        {
            // Literal block style (|)
            emitter.Emit(new Scalar(
                AnchorName.Empty,
                TagName.Empty,
                s,
                ScalarStyle.Literal,
                true,
                false));
        }
        else
        {
            emitter.Emit(new Scalar(s));
        }
    }
}