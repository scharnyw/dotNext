.NEXT
====
[![Build Status](https://dev.azure.com/rvsakno/dotNext/_apis/build/status/sakno.dotNext?branchName=master)](https://dev.azure.com/rvsakno/dotNext/_build/latest?definitionId=1&branchName=master)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/sakno/dotNext/blob/master/LICENSE)
![Test Coverage](https://img.shields.io/azure-devops/coverage/rvsakno/dotnext/1/master)

.NEXT (dotNext) is a set of powerful libraries aimed to improve development productivity and extend .NET API with unique features. Some of these features are planned in future releases of .NET platform but already implemented in the library:

| Proposal | Implementation |
| ---- | ---- |
| [Static Delegates](https://github.com/dotnet/csharplang/blob/master/proposals/static-delegates.md) | [Value Delegates](https://sakno.github.io/dotNext/features/core/valued.html) |
| [Operators for IntPtr and UIntPtr](https://github.com/dotnet/corefx/issues/32775) | [Extension methods](https://sakno.github.io/dotNext/api/DotNext.ValueTypeExtensions.html) for arithmetic, bitwise and comparison operations |
| [Enum API](https://github.com/dotnet/corefx/issues/34077) | [Documentation](https://sakno.github.io/dotNext/features/core/enum.html) |
| [Check if an instance of T is a default(T)](https://github.com/dotnet/corefx/issues/16209) | [IsDefault() method](https://sakno.github.io/dotNext/api/DotNext.Runtime.Intrinsics.html#DotNext_Runtime_Intrinsics_IsDefault__1___0_) |
| [Concept Types](https://github.com/dotnet/csharplang/issues/110) | [Documentation](https://sakno.github.io/dotNext/features/concept.html) |
| [Expression Trees covering additional language constructs](https://github.com/dotnet/csharplang/issues/158), i.e. `foreach`, `await`, patterns, multi-line lambda expressions | [Metaprogramming](https://sakno.github.io/dotNext/features/metaprogramming/index.html) |
| [Async Locks](https://github.com/dotnet/corefx/issues/34073) | [Documentation](https://sakno.github.io/dotNext/features/threading/index.html) |
| [High-performance general purpose Write-Ahead Log](https://github.com/dotnet/corefx/issues/25034) | [Persistent Log](https://sakno.github.io/dotNext/features/cluster/wal.html)  |
| [Memory-mapped file as Memory&lt;byte&gt;](https://github.com/dotnet/runtime/issues/37227) | [MemoryMappedFileExtensions](https://sakno.github.io/dotNext/features/io/mmfile.html) |
| [Memory-mapped file as ReadOnlySequence&lt;byte&gt;](https://github.com/dotnet/runtime/issues/24805) | [ReadOnlySequenceAccessor](https://sakno.github.io/dotNext/api/DotNext.IO.MemoryMappedFiles.ReadOnlySequenceAccessor.html) |

Quick overview of additional features:

* [Attachment of user data to arbitrary objects](https://sakno.github.io/dotNext/features/core/userdata.html)
* [Automatic generation of Equals/GetHashCode](https://sakno.github.io/dotNext/features/core/autoeh.html) for arbitrary type at runtime which is much better that Visual Studio compile-time helper for generating these methods
* Extended set of [atomic operations](https://sakno.github.io/dotNext/features/core/atomic.html). Inspired by [AtomicInteger](https://docs.oracle.com/javase/10/docs/api/java/util/concurrent/atomic/AtomicInteger.html) and friends from Java
* [Fast Reflection](https://sakno.github.io/dotNext/features/reflection/fast.html)
* Fast conversion of bytes to hexadecimal representation and vice versa using `ToHex` and `FromHex` methods from [Span](https://sakno.github.io/dotNext/api/DotNext.Span.html) static class
* `ManualResetEvent`, `ReaderWriterLockSlim` and other synchronization primitives now have their [asynchronous versions](https://sakno.github.io/dotNext/features/threading/rwlock.html)
* [Atomic](https://sakno.github.io/dotNext/features/core/atomic.html) memory access operations for arbitrary value types including enums
* [PipeExtensions](https://sakno.github.io/dotNext/api/DotNext.IO.Pipelines.PipeExtensions.html) provides high-level I/O operations for pipelines such as string encoding and decoding
* Fully-featured [Raft implementation](https://github.com/sakno/dotNext/tree/master/src/cluster)

All these things are implemented in 100% managed code on top of existing .NET Standard stack without modifications of Roslyn compiler or CoreFX libraries.

# Quick Links

* [Features](https://sakno.github.io/dotNext/features/core/index.html)
* [API documentation](https://sakno.github.io/dotNext/api/DotNext.html)
* [Benchmarks](https://sakno.github.io/dotNext/benchmarks.html)
* [NuGet Packages](https://www.nuget.org/profiles/rvsakno)

Documentation for older versions:
* [1.x](https://sakno.github.io/dotNext/versions/1.x/index.html)

# What's new
Release Date: 11-01-2020

<a href="https://www.nuget.org/packages/dotnext/2.11.0">DotNext 2.11.0</a>
* Added `Span<T>.CopyTo` and `ReadOnlySpan<T>.CopyTo` extension methods to support cases when the source span can be larger than the destination
* Added `Span.AsSpan` and `Span.AsReadOnlySpan` for value tuples
* Deprecated [EnumerableTuple](https://sakno.github.io/dotNext/api/DotNext.EnumerableTuple-2.html) data type
* Minor performance improvements
* Updated dependencies

<a href="https://www.nuget.org/packages/dotnext.metaprogramming/2.11.0">DotNext.Metaprogramming 2.11.0</a>
* Overloaded `CodeGenerator.AsyncLambda` supports _Pascal_-style return (issue [13](https://github.com/sakno/dotNext/issues/13))
* Fixed suppression of exceptions raised by generated async lambda (issue [14](https://github.com/sakno/dotNext/issues/14))
* Fixed invalid behavior of async lambda body rewriter (issue [17](https://github.com/sakno/dotNext/issues/17))
* Updated dependencies

<a href="https://www.nuget.org/packages/dotnext.reflection/2.11.0">DotNext.Reflection 2.11.0</a>
* Updated dependencies

<a href="https://www.nuget.org/packages/dotnext.threading/2.11.0">DotNext.Threading 2.11.0</a>
* Updated dependencies

<a href="https://www.nuget.org/packages/dotnext.unsafe/2.11.0">DotNext.Unsafe 2.11.0</a>
* Updated dependencies

<a href="https://www.nuget.org/packages/dotnext.net.cluster/2.11.0">DotNext.Net.Cluster 2.11.0</a>
* Added `requestTimeout` configuration property for TCP/UDP transports
* Stabilized shutdown of Raft server for TCP/UDP transports
* Added SSL support for TCP transport
* Updated dependencies shipped with .NET Core 3.1.9

<a href="https://www.nuget.org/packages/dotnext.aspnetcore.cluster/2.11.0">DotNext.AspNetCore.Cluster 2.11.0</a>
* Added `requestTimeout` and `rpcTimeout` configuration properties for precise control over timeouts used for communication between Raft nodes (issue [12](https://github.com/sakno/dotNext/issues/12))
* Updated dependencies shipped with .NET Core 3.1.9

Changelog for previous versions located [here](./CHANGELOG.md).

# Release Policy
* The libraries are versioned according with [Semantic Versioning 2.0](https://semver.org/).
* Version 0.x and 1.x relies on .NET Standard 2.0
* Version 2.x relies on .NET Standard 2.1

# Support Policy
| Version | .NET compatibility | Support Level |
| ---- | ---- | ---- |
| 0.x | .NET Standard 2.0 | Not Supported |
| 1.x | .NET Standard 2.0 | Maintenance |
| 2.x | .NET Standard 2.1 | Active Development |

_Maintenance_ support level means that new releases will contain bug fixes only.

[DotNext.AspNetCore.Cluster](https://www.nuget.org/packages/DotNext.AspNetCore.Cluster/) of version 1.x is no longer supported because of ASP.NET Core 2.2 end-of-life.

[DotNext.Net.Cluster](https://www.nuget.org/packages/DotNext.Net.Cluster/) of version 1.x is no longer supported due to few reasons:
1. Underlying implementation for ASP.NET Core is no longer supported
1. Raft implementation is incomplete

# Development Process
Philosophy of development process:
1. All libraries in .NEXT family based on .NET Standard to be available for wide range of .NET implementations: Mono, Xamarin, .NET Core
1. Compatibility with AOT compiler should be checked for every release
1. Minimize set of dependencies
1. Rely on .NET Standard specification
1. Provide high-quality documentation
1. Stay cross-platform
1. Provide benchmarks
