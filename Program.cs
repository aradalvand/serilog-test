using NodaTime;
using Serilog;
using Serilog.Core;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
            .Enrich.With<TypeEnricher>()
        .WriteTo.Console(
            outputTemplate: "({Level:u4}) [{SourceContext:l}] <{Timestamp:HH:mm:ss.fff}>:{NewLine}{Message:lj}{NewLine}{Exception}---{NewLine}",
            formatProvider: new GuidFormatter()
        )
        .Destructure.AsScalar<Guid>()
        .Destructure.ByTransforming<Type>(e => e.Name)
        // .Destructure.AsScalar<Type>()
        .Destructure.AsScalar<Instant>()
        .Destructure.AsScalar<CustomClass>()
        // .Destructure.With<GuidDestructuringPolicy>()
        .CreateLogger();

Log.Information("Hello, {Guid}!", Guid.NewGuid());
Log.Information("Hello, {CustomClass}!", new CustomClass { I = 1 });
Log.Information("Hello, {CustomStruct}!", new CustomStruct());
Log.Information("Hello, {CustomStruct}!", typeof(string));
Log.Information("Hello, {Instant}!", SystemClock.Instance.GetCurrentInstant());

static class Helper
{
    public static object ToString(Guid guid)
    {
        return guid.ToString().Substring(0, 5);
    }

    public static object ToString(CustomClass zxc)
    {
        return $"zxc: {zxc.I}";
    }

    public static object ToString(CustomStruct asd)
    {
        return $"asd: {asd.J}";
    }
}

public class TypeEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var property in logEvent.Properties)
        {
            System.Console.WriteLine("FUCK");
            if (property.Value is ScalarValue scalar && scalar.Value is Type type)
            {
                logEvent.AddOrUpdateProperty(
                    propertyFactory.CreateProperty(
                        property.Key,
                        type.Name,
                        true));
            }
        }
    }
}

readonly struct CustomStruct
{
    public int J { get; }
}

class CustomClass
{
    public int I { get; set; }
}


public class GuidDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        Console.WriteLine($"DESTRUCTING: `{value}`");
        if (value is Instant instant)
        {
            var formatted = instant.InZone(DateTimeZoneProviders.Tzdb["Asia/Tehran"]).ToDateTimeOffset().ToString("yyyy-MM-dd HH:mm:ss");
            result = propertyValueFactory.CreatePropertyValue(formatted);
            return true;
        }

        result = default;
        return false;
    }
}

class GuidFormatter : IFormatProvider, ICustomFormatter
{
    public string Format(string? format, object? arg, IFormatProvider? formatProvider)
    {
        if (arg is Instant instant)
        {
            var formatted = instant.InZone(DateTimeZoneProviders.Tzdb["Asia/Tehran"]).ToDateTimeOffset().ToString("yyyy-MM-dd HH:mm:ss zzz");
            return formatted;
        }
        if (arg is Guid guid)
        {
            return guid.ToString()[^12..];
        }
        if (arg is Type type)
        {
            return type.Name;
        }

        Console.Error.Write($"(INCOMING ARG: `{arg}` <{arg.GetType()}>)");
        return arg?.ToString() ?? "NULL";
    }

    public object? GetFormat(Type formatType)
    {
        return formatType == typeof(ICustomFormatter)
            ? this
            : null;
    }
}
