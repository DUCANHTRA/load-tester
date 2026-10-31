using LoadTester;

var result = ArgumentParser.Parse(args);

if (!result.IsSuccess)
{
    Console.Error.WriteLine($"Error: {result.Error}");
    Console.Error.WriteLine();
    Console.Error.WriteLine(Usage.Text);
    return 1;
}

var options = result.Options!;

Console.WriteLine($"Target:       {options.Url}");
Console.WriteLine($"Requests:     {options.Requests}");
Console.WriteLine($"Concurrency:  {options.Concurrency}");
Console.WriteLine($"Timeout:      {options.TimeoutSeconds}s");

return 0;