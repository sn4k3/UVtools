/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Collections;
using System.Collections.Generic;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Structures;

public sealed class BenchmarkMachine: IReadOnlyList<BenchmarkTestResult>
{
    public string CPU { get; init; }
    public string RAM { get; init; }

    public BenchmarkTestResult[] Tests { get; init; }

    public BenchmarkMachine(string cpu, string ram, BenchmarkTestResult[] tests)
    {
        CPU = cpu;
        RAM = ram;
        Tests = tests;
    }

    public override string ToString()
    {
        return $"{CPU}\n{RAM}";
    }

    public IEnumerator<BenchmarkTestResult> GetEnumerator()
    {
        return (IEnumerator<BenchmarkTestResult>)Tests.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Tests.GetEnumerator();
    }

    public int Count => Tests.Length;

    public BenchmarkTestResult this[int index] => Tests[index];
}

public sealed class BenchmarkTestResult
{
    public float SingleThreadResult { get; init; }
    public float MultiThreadResult { get; init; }

    public BenchmarkTestResult()
    {
    }

    public BenchmarkTestResult(float singleThreadResult, float multiThreadResult)
    {
        SingleThreadResult = singleThreadResult;
        MultiThreadResult = multiThreadResult;
    }
}

public sealed class BenchmarkTest
{
    public BenchmarkTest(string name, string functionName, BenchmarkWindow.BenchmarkResolution resolution)
    {
        Name = name;
        FunctionName = functionName;
        Resolution = resolution;
    }

    public string Name { get; }
    public string FunctionName { get; }

    public BenchmarkWindow.BenchmarkResolution Resolution { get; }

    public override string ToString()
    {
        return Name;
    }
}