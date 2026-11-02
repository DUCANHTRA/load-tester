namespace LoadTester.Tests;

public class ArgumentParserTests
{
    [Fact]
    public void Good_input_gives_options()
    {
        var result = ArgumentParser.Parse(["https://example.com", "-n", "500", "-c", "20"]);

        Assert.True(result.IsSuccess);
        Assert.Equal(new Uri("https://example.com"), result.Options!.Url);
        Assert.Equal(500, result.Options.Requests);
        Assert.Equal(20, result.Options.Concurrency);
    }

    [Fact]
    public void Missing_flags_use_defaults()
    {
        var result = ArgumentParser.Parse(["https://example.com"]);

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Options!.Requests);
        Assert.Equal(10, result.Options.Concurrency);
        Assert.Equal(10, result.Options.TimeoutSeconds);
    }

    [Fact]
    public void Missing_url_fails()
    {
        var result = ArgumentParser.Parse([]);

        Assert.False(result.IsSuccess);
        Assert.Contains("Missing URL", result.Error);
    }

    [Fact]
    public void Invalid_url_fails()
    {
        var result = ArgumentParser.Parse(["example.com"]);

        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("abc")]
    public void Bad_request_count_fails(string value)
    {
        var result = ArgumentParser.Parse(["https://example.com", "-n", value]);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Flag_without_value_fails()
    {
        var result = ArgumentParser.Parse(["https://example.com", "-n"]);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Unknown_flag_fails()
    {
        var result = ArgumentParser.Parse(["https://example.com", "-x", "5"]);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Concurrency_is_lowered_to_request_count()
    {
        var result = ArgumentParser.Parse(["https://example.com", "-n", "5", "-c", "20"]);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Options!.Concurrency);
    }

    [Fact]
    public void Json_flag_turns_json_on()
    {
        var result = ArgumentParser.Parse(["https://example.com", "--json", "-n", "5"]);

        Assert.True(result.IsSuccess);
        Assert.True(result.Options!.Json);
        Assert.Equal(5, result.Options.Requests);
    }

    [Fact]
    public void Json_is_off_by_default()
    {
        var result = ArgumentParser.Parse(["https://example.com"]);

        Assert.False(result.Options!.Json);
    }
}
