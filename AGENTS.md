# Repository Agent Instructions

## Performance

When reviewing, improving, or writing code, proactively look for avoidable allocations, copies, repeated enumeration,
and unnecessary data structure growth. Apply appropriate performance improvements without requiring the user to request
them explicitly.

For hot paths, parsers, encoders, serializers, mesh processing, image processing, and tight loops:

- Prefer `Span<T>`, `ReadOnlySpan<T>`, and span-based framework APIs over temporary arrays, substrings, and copied
  collections.
- Use `stackalloc` for small, fixed-size or strictly bounded temporary buffers. Never stack-allocate buffers sized
  directly from untrusted or potentially large input; use a conservative size threshold and fall back to pooled or
  heap-backed storage.
- Prefer allocation-free formatting and binary APIs such as `Utf8Formatter`, `TryFormat`, and `BinaryPrimitives` where
  they fit the existing code. Follow `UVtools.Core/MeshFormats/MeshTextWriter.cs` as a local example.
- In projects that already reference DotNext, prefer the appropriate DotNext buffer writer when practical instead of
  `StringBuilder`, `List<T>`, `MemoryStream`, repeated array resizing, or large temporary allocations. Select it using
  this table:

  | Buffer writer | When to use | Async compatible | Write space complexity |
  |---|---|---:|---:|
  | `PoolingArrayBufferWriter<T>` | General use when the initial capacity is known. | Yes | `o(1)`, `O(n)` |
  | `PoolingBufferWriter<T>` | A custom memory allocator is required, such as an unmanaged memory pool. | Yes | `o(1)`, `O(n)` |
  | `BufferWriterSlim<T>` | The optimal initial buffer size is known and can be allocated on the stack, avoiding a rented buffer and a managed-heap allocation for the writer itself. | No | `o(1)`, `O(n)` |
  | `SparseBufferWriter<T>` | The optimal initial buffer size is unknown and the written length varies widely. | Yes | `o(1)`, `O(1)` |

  Prefer `BufferWriterSlim<T>` whenever the work is synchronous and a small, bounded stack buffer is practical. Treat
  it as the default DotNext writer; use `PoolingArrayBufferWriter<T>`, `PoolingBufferWriter<T>`, or
  `SparseBufferWriter<T>` only when their async compatibility, ownership or allocator requirements, expected buffer
  lifetime, or substantially better growth characteristics make them a more appropriate choice.

  Use `MemoryOwner<T>` with `ArrayPool<T>` and other suitable DotNext pooling or ownership primitives when data must be
  retained or transferred beyond the writer's lifetime.
- Dispose pooled owners and other resources deterministically, and do not allow spans or stack-backed data to escape
  their valid lifetime.
- Reuse established repository patterns before introducing new abstractions or dependencies. Do not add DotNext to
  another project solely for a speculative micro-optimization.
- Keep correctness, bounds safety, readability, and measured impact ahead of allocation reduction. For non-obvious or
  invasive changes, validate with representative benchmarks or profiling.
