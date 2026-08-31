using Xunit;

namespace Mz.Toml.Tests;

public sealed class TomlTemporalTests
{
    [Fact]
    public void Parses_Offset_DateTime()
    {
        var document = Toml.Parse("value = 1979-05-27T07:32:00-07:00\n");

        var value = document.Root.AsValue("value");

        Assert.Equal(TomlValueKind.OffsetDateTime, value.ValueKind);

        var temporal = value.AsOffsetDateTime();

        Assert.Equal(1979, temporal.Date.Year);
        Assert.Equal(5, temporal.Date.Month);
        Assert.Equal(27, temporal.Date.Day);
        Assert.Equal(7, temporal.Time.Hour);
        Assert.Equal(32, temporal.Time.Minute);
        Assert.Equal(0, temporal.Time.Second);
        Assert.Equal(-420, temporal.OffsetMinutes);
    }

    [Theory]
    [InlineData("1987-07-05T17:45:00Z")]
    [InlineData("1987-07-05t17:45:00z")]
    [InlineData("1987-07-05 17:45:00Z")]
    public void Offset_DateTime_Accepts_Toml_10_Separators(string text)
    {
        var document = Toml.Parse($"value = {text}\n");
            
        Assert.Equal("value = 1987-07-05T17:45:00Z\n", Toml.Write(document));
    }

    [Fact]
    public void Parses_Local_DateTime()
    {
        var document = Toml.Parse("value = 1977-12-21T10:32:00.555\n");
        var value = document.Root.AsValue("value");

        Assert.Equal(TomlValueKind.LocalDateTime, value.ValueKind);

        var temporal = value.AsLocalDateTime();
        Assert.Equal("555", temporal.Time.FractionalSeconds);
    }

    [Fact]
    public void Parses_Local_Date()
    {
        var document = Toml.Parse("value = 1987-07-05\n");
        var value = document.Root.AsValue("value");

        Assert.Equal(TomlValueKind.LocalDate, value.ValueKind);
        Assert.Equal(1987, value.AsLocalDate().Year);
    }

    [Fact]
    public void Parses_Local_Time()
    {
        var document = Toml.Parse("value = 10:32:00.555\n");
        var value = document.Root.AsValue("value");

        Assert.Equal(TomlValueKind.LocalTime, value.ValueKind);
        Assert.Equal("555", value.AsLocalTime().FractionalSeconds);
    }

    [Fact]
    public void Preserves_Arbitrary_Fractional_Precision()
    {
        var document = Toml.Parse("value = 10:32:00.12345678901234567890\n");
        var value = document.Root.AsValue("value");

        Assert.Equal("12345678901234567890", value.AsLocalTime().FractionalSeconds);
        Assert.Equal("value = 10:32:00.12345678901234567890\n", Toml.Write(document));
    }

    [Fact]
    public void Fraction_Is_Not_Rounded_Or_Expanded()
    {
        var document = Toml.Parse("value = 1987-07-05T17:45:56.6Z\n");
        var value = document.Root.AsValue("value");

        Assert.Equal("6", value.AsOffsetDateTime().Time.FractionalSeconds);
        Assert.Equal("value = 1987-07-05T17:45:56.6Z\n", Toml.Write(document));
    }

    [Fact]
    public void Leap_Day_Is_Validated()
    {
        Assert.True(Toml.TryParse("a = 2000-02-29\n").IsSuccess);
        Assert.False(Toml.TryParse("a = 2100-02-29\n").IsSuccess);
    }

    [Theory]
    [InlineData("0001-01-01")]
    [InlineData("9999-12-31")]
    public void Local_Date_Supports_Toml_Year_Bounds(string text) 
        => Assert.True(Toml.TryParse($"a = {text}\n").IsSuccess);

    [Theory]
    [InlineData("10000-01-01")]
    [InlineData("2000-00-01")]
    [InlineData("2000-13-01")]
    [InlineData("2000-01-00")]
    [InlineData("2000-01-32")]
    [InlineData("2001-02-29")]
    public void Invalid_Local_Dates_Are_Rejected(string text) 
        => Assert.False(Toml.TryParse($"a = {text}\n").IsSuccess);

    [Theory]
    [InlineData("24:00:00")]
    [InlineData("00:60:00")]
    [InlineData("00:00:61")]
    [InlineData("1:00:00")]
    [InlineData("01:0:00")]
    [InlineData("01:00:0")]
    [InlineData("01:00:00.")]
    public void Invalid_Local_Times_Are_Rejected(string text) 
        => Assert.False(Toml.TryParse($"a = {text}\n").IsSuccess);

    [Fact]
    public void Leap_Second_Spelling_Is_Preserved()
    {
        var document = Toml.Parse("value = 23:59:60\n");
        var value = document.Root.AsValue("value");

        Assert.Equal(60, value.AsLocalTime().Second);
        Assert.Equal("value = 23:59:60\n", Toml.Write(document));
    }

    [Theory]
    [InlineData("+23:59", 1439)]
    [InlineData("-23:59", -1439)]
    public void Supports_Full_Toml_Numeric_Offset_Field_Range(string offset, int expectedMinutes) 
    {
        var document = Toml.Parse($"a = 2000-01-01T00:00:00{offset}\n");
        var value = document.Root.AsValue("a");

        Assert.Equal(expectedMinutes, value.AsOffsetDateTime().OffsetMinutes);
    }

    [Theory]
    [InlineData("+24:00")]
    [InlineData("+00:60")]
    [InlineData("+01")]
    [InlineData("+0100")]
    [InlineData("+01:0")]
    public void Invalid_Offsets_Are_Rejected(string offset) 
        => Assert.False(Toml.TryParse($"a = 2000-01-01T00:00:00{offset}\n").IsSuccess);

    [Theory]
    [InlineData("13:37")]
    [InlineData("1979-05-27T07:32")]
    [InlineData("1979-05-27 07:32Z")]
    [InlineData("1979-05-27 07:32-07:00")]
    public void Toml_11_Optional_Seconds_Remain_Rejected(string text) 
        => Assert.False(Toml.TryParse($"a = {text}\n").IsSuccess);

    [Fact]
    public void Temporal_Values_Work_Inside_Array()
    {
        var document = Toml.Parse("values = [1987-07-05T17:45:00Z, 1979-05-27T07:32:00, 2006-06-01, 11:00:00]\n");
        var array = document.Root.AsArray("values");

        Assert.Equal(TomlValueKind.OffsetDateTime, array.AsValue(0).ValueKind);
        Assert.Equal(TomlValueKind.LocalDateTime, array.AsValue(1).ValueKind);
        Assert.Equal(TomlValueKind.LocalDate, array.AsValue(2).ValueKind);
        Assert.Equal(TomlValueKind.LocalTime, array.AsValue(3).ValueKind);
    }

    [Fact]
    public void Temporal_Values_Work_Inside_Inline_Table()
    {
        var document = Toml.Parse("value = { date = 2006-06-01, time = 11:00:00 }\n");
        var table = document.Root.AsTable("value");

        Assert.Equal(TomlValueKind.LocalDate, table.AsValue("date").ValueKind);
        Assert.Equal(TomlValueKind.LocalTime, table.AsValue("time").ValueKind);
    }

    [Fact]
    public void Programmatic_Temporal_Values_Write_Deterministically()
    {
        var document = new TomlDocument();

        document.Root.Set("date", TomlValue.FromLocalDate(new TomlLocalDate(2006, 6, 1)));
        document.Root.Set("time", TomlValue.FromLocalTime(new TomlLocalTime(11, 0, 0, "1250")));

        Assert.Equal("""
            date = 2006-06-01
            time = 11:00:00.1250

            """.ReplaceLineEndings("\n"),
            Toml.Write(document));
    }

    [Fact]
    public void Rfc3339_Year_Zero_Is_Preserved()
    {
        var document = Toml.Parse("value = 0000-01-01\n");
        var value = document.Root.AsValue("value");

        Assert.Equal(0, value.AsLocalDate().Year);
        Assert.Equal("value = 0000-01-01\n", Toml.Write(document));
    }

    [Fact]
    public void Negative_Zero_Offset_Preserves_Unknown_Local_Offset()
    {
        var document = Toml.Parse("value = 2000-01-01T00:00:00-00:00\n");
        var value = document.Root.AsValue("value").AsOffsetDateTime();

        Assert.Equal(0, value.OffsetMinutes);
        Assert.True(value.IsUnknownLocalOffset);
        Assert.Equal("value = 2000-01-01T00:00:00-00:00\n", Toml.Write(document));
    }

    [Fact]
    public void Positive_Zero_Offset_Is_Known_And_Canonicalizes_To_Z()
    {
        var document = Toml.Parse("value = 2000-01-01T00:00:00+00:00\n");
        var value = document.Root.AsValue("value").AsOffsetDateTime();

        Assert.Equal(0, value.OffsetMinutes);
        Assert.False(value.IsUnknownLocalOffset);
        Assert.Equal("value = 2000-01-01T00:00:00Z\n", Toml.Write(document));
    }

    [Fact]
    public void Programmatic_Unknown_Local_Offset_Writes_Negative_Zero()
    {
        var document = new TomlDocument();
        var date = new TomlLocalDate(2000, 1, 1);
        var time = new TomlLocalTime(0, 0, 0);
        var dateTime = new TomlOffsetDateTime(date, time, 0, true);

        document.Root.Set("value", TomlValue.FromOffsetDateTime(dateTime));

        Assert.Equal("value = 2000-01-01T00:00:00-00:00\n", Toml.Write(document));
    }    
}
