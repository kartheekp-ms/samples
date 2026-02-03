// See https://aka.ms/new-console-template for more information
using System.IO.Pipelines;
using PipeJsonDemo;

var pipe = new Pipe();

var input = new Person("Ada", 37);
await PipeJsonSerializer.WriteJsonToPipeAsync(pipe.Writer, input);

var output = await PipeJsonSerializer.ReadJsonFromPipeAsync<Person>(pipe.Reader);
Console.WriteLine($"Round-trip: {output?.Name} ({output?.Age})");

