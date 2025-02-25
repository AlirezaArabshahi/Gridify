using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;
using Fop;
using Fop.FopExpression;
using Gridify;
using Gridify.Tests;
using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;

namespace Benchmarks
{
   [MemoryDiagnoser]
   [RPlotExporter]
   [Orderer(SummaryOrderPolicy.FastestToSlowest)]
   public class LibraryComparisionFilteringBenchmark
   {
      private static readonly Consumer Consumer = new();
      private TestClass[] _data;
      private Func<TestClass, bool> compiled1;
      private Func<TestClass, bool> compiled2;
      private Func<TestClass, bool> compiled3;

      private IQueryable<TestClass> Ds => _data.AsQueryable();
      private IEnumerable<TestClass> EnumerableDs => _data.ToList();
      private IGridifyMapper<TestClass> gm { get; set; }

      [GlobalSetup]
      public void Setup()
      {
         _data = GetSampleData().ToArray();

         gm = new GridifyMapper<TestClass>().GenerateMappings();

         // compiled query (this is not included in our readme benchmarks)
         var gq1 = new GridifyQuery() { Filter = "Name=*a" };
         var gq2 = new GridifyQuery() { Filter = "Id>5" };
         var gq3 = new GridifyQuery() { Filter = "Name=Ali" };
         compiled1 = gq1.GetFilteringExpression(gm).Compile();
         compiled2 = gq2.GetFilteringExpression(gm).Compile();
         compiled3 = gq3.GetFilteringExpression(gm).Compile();
      }


      [Benchmark(Baseline = true)]
      public void NativeLinQ()
      {
         Ds.Where(q => q.Name.Contains("a")).Consume(Consumer);
         Ds.Where(q => q.Id > 5).Consume(Consumer);
         Ds.Where(q => q.Name == "Ali").Consume(Consumer);
      }

      [Benchmark]
      public void Gridify()
      {
         Ds.ApplyFiltering("Name=*a", gm).Consume(Consumer);
         Ds.ApplyFiltering("Id>5", gm).Consume(Consumer);
         Ds.ApplyFiltering("Name=Ali", gm).Consume(Consumer);
      }

      // [Benchmark] // compiled query (this is not included in our readme benchmarks)w
      public void GridifyCompiled()
      {
         EnumerableDs.Where(compiled1).Consume(Consumer);
         EnumerableDs.Where(compiled2).Consume(Consumer);
         EnumerableDs.Where(compiled3).Consume(Consumer);
      }

      [Benchmark]
      public void Fop()
      {
         // fop doesn't have filtering only feature
         Ds.ApplyFop(FopExpressionBuilder<TestClass>.Build("Name~=a", "Name", 1, 1000)).Item1.Consume(Consumer);
         Ds.ApplyFop(FopExpressionBuilder<TestClass>.Build("Id>5", "Name", 1, 1000)).Item1.Consume(Consumer);
         Ds.ApplyFop(FopExpressionBuilder<TestClass>.Build("Name==Ali", "Name", 1, 1000)).Item1.Consume(Consumer);
      }

      [Benchmark]
      public void DynamicLinQ()
      {
         Ds.Where("Name.Contains(@0)", "a").Consume(Consumer);
         Ds.Where("Id > (@0)", "5").Consume(Consumer);
         Ds.Where("Name==(@0)", "Ali").Consume(Consumer);
      }

      [Benchmark]
      public void Sieve()
      {
         var processor = new SieveProcessor(new OptionsWrapper<SieveOptions>(new SieveOptions()));
         processor.Apply(new SieveModel { Filters = "Name@=a" }, Ds, applySorting: false, applyPagination: false).Consume(Consumer);
         processor.Apply(new SieveModel { Filters = "Id>5" }, Ds, applySorting: false, applyPagination: false).Consume(Consumer);
         processor.Apply(new SieveModel { Filters = "Name==Ali" }, Ds, applySorting: false, applyPagination: false).Consume(Consumer);
      }


      public static IEnumerable<TestClass> GetSampleData()
      {
         var lst = new List<TestClass>();
         lst.Add(new TestClass(1, "John", null, Guid.NewGuid(), DateTime.Now));
         lst.Add(new TestClass(2, "Bob", null, Guid.NewGuid(), DateTime.UtcNow));
         lst.Add(new TestClass(3, "Jack", (TestClass)lst[0].Clone(), Guid.Empty, DateTime.Now.AddDays(2)));
         lst.Add(new TestClass(4, "Rose", null, Guid.Parse("e2cec5dd-208d-4bb5-a852-50008f8ba366")));
         lst.Add(new TestClass(5, "Ali", null));
         lst.Add(new TestClass(6, "Hamid", (TestClass)lst[0].Clone(), Guid.Parse("de12bae1-93fa-40e4-92d1-2e60f95b468c")));
         lst.Add(new TestClass(7, "Hasan", (TestClass)lst[1].Clone()));
         lst.Add(new TestClass(8, "Farhad", (TestClass)lst[2].Clone(), Guid.Empty));
         lst.Add(new TestClass(9, "Sara", null));
         lst.Add(new TestClass(10, "Jorge", null));
         lst.Add(new TestClass(11, "joe", null));
         lst.Add(new TestClass(12, "jimmy", (TestClass)lst[0].Clone()));
         lst.Add(new TestClass(13, "Nazanin", null));
         lst.Add(new TestClass(14, "Reza", null));
         lst.Add(new TestClass(15, "Korosh", (TestClass)lst[0].Clone()));
         lst.Add(new TestClass(16, "Kamran", (TestClass)lst[1].Clone()));
         lst.Add(new TestClass(17, "Saeid", (TestClass)lst[2].Clone()));
         lst.Add(new TestClass(18, "jessi==ca", null));
         lst.Add(new TestClass(19, "Ped=ram", null));
         lst.Add(new TestClass(20, "Peyman!", null));
         lst.Add(new TestClass(21, "Fereshte", null));
         lst.Add(new TestClass(22, "LIAM", null));
         lst.Add(new TestClass(22, @"\Liam", null));
         lst.Add(new TestClass(23, "LI | AM", null));
         lst.Add(new TestClass(24, "(LI,AM)", null));
         return lst;
      }
   }
}

/* Last Run:
 BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.1237 (21H1/May2021Update)
11th Gen Intel Core i5-11400F 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=5.0.301
[Host]     : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT
DefaultJob : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT


|      Method |       Mean |    Error |   StdDev | Ratio |   Gen 0 |   Gen 1 | Allocated |
|------------ |-----------:|---------:|---------:|------:|--------:|--------:|----------:|
| Native LINQ |   740.9 us |  7.80 us |  6.92 us |  1.00 |  5.8594 |  2.9297 |     37 KB |
|     Gridify |   762.6 us | 10.06 us |  9.41 us |  1.03 |  5.8594 |  2.9297 |     39 KB |
| DynamicLinq |   902.1 us | 11.56 us | 10.81 us |  1.22 | 19.5313 |  9.7656 |    122 KB |
|       Sieve |   977.9 us |  6.80 us |  6.37 us |  1.32 |  7.8125 |  3.9063 |     54 KB |
|         Fop | 2,959.8 us | 39.11 us | 36.58 us |  3.99 | 46.8750 | 23.4375 |    306 KB |
*/
