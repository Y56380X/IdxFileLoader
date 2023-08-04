// Copyright Y56380X https://github.com/Y56380X/IdxFileLoader.
// Licensed under the MIT License.

namespace IdxFileLoader

open System
open System.Drawing
open System.IO
open Microsoft.FSharp.Collections
open Microsoft.FSharp.Core

type internal IdxInfo = { DataType: Type; DimensionCounts: int array }

type IdxFile internal (stream: Stream, info: IdxInfo) =
    static let loadInfoAsync (stream: Stream) = task {
        let headerBuffer = Array.zeroCreate 4
        do! stream.ReadExactlyAsync (headerBuffer.AsMemory ())
        let header =
           match headerBuffer with
           | [|0uy; 0uy; 8uy; dc |] -> {| DataType = typeof<byte>; DimensionCount = int dc |}
           | [|0uy; 0uy; dt ; _  |] -> failwith $"Data type {dt} currently not supported."
           | _                      -> failwith "Magic number format error."
        let countsBuffer = Array.zeroCreate (4 * header.DimensionCount)
        do! stream.ReadExactlyAsync (countsBuffer.AsMemory ())
        let msbInt32 (bs: byte Memory) =
            if BitConverter.IsLittleEndian
            then bs.Span.Reverse ()
            BitConverter.ToInt32 bs.Span
        let dimensionCounts = [|for i in [0..header.DimensionCount - 1] do countsBuffer.AsMemory (i * 4, 4) |> msbInt32 |]
        return {
            DataType = header.DataType
            DimensionCounts = dimensionCounts
        }
    }
    static member LoadAsync (stream: Stream) = task {
        let! info = loadInfoAsync stream
        let idxFile : IdxFile =
            match info.DimensionCounts.Length with
            | 1 -> new IdxLabelFile (stream, info)
            | 3 -> new IdxImageFile (stream, info)
            | _ -> new IdxFile      (stream, info)
        return idxFile
    }
    static member LoadAsync (path: string) = task {
        let file = File.OpenRead path
        let! idxFile =
            try IdxFile.LoadAsync file
            with e -> file.Dispose ()
                      reraise ()
        return idxFile
    }
    member val ItemCount  = info.DimensionCounts[0]
    member val BufferSize = info.DimensionCounts |> Seq.skip 1 |> Seq.fold (*) 1
    member this.ReadDataSegmentAsync (buffer: byte Memory) = stream.ReadExactlyAsync buffer
    member this.ReadDataSegmentAsync () = task {
        let buffer = Array.zeroCreate this.BufferSize
        do! this.ReadDataSegmentAsync (buffer.AsMemory ())
        return buffer
    }
    interface IDisposable with
        member this.Dispose() = stream.Dispose ()
    interface IAsyncDisposable with
        member this.DisposeAsync() = stream.DisposeAsync ()

and IdxImageFile internal (stream, info) =
    inherit IdxFile (stream, info)
    member val ImageCount = info.DimensionCounts[0]
    member val ImageSize = Size (info.DimensionCounts[2], info.DimensionCounts[1])
    member this.ReadImageBufferAsync (buffer: byte Memory) = this.ReadDataSegmentAsync buffer
    member this.ReadImageBufferAsync () = task {
        let! buffer = this.ReadDataSegmentAsync ()
        return Array2D.init
            this.ImageSize.Height this.ImageSize.Width
            (fun y x -> buffer[y * this.ImageSize.Height + x])
    }
    static member LoadAsync (path: string) = task {
        let! idxFile = IdxFile.LoadAsync path
        return idxFile :?> IdxImageFile
    }
    static member LoadAsync (stream: Stream) = task {
        let! idxFile = IdxFile.LoadAsync stream
        return idxFile :?> IdxImageFile
    }

and IdxLabelFile internal (stream, info) =
    inherit IdxFile (stream, info)
    member val LabelCount = info.DimensionCounts[0]
    member this.ReadLabelAsync () = task {
        let! buffer = this.ReadDataSegmentAsync ()
        return buffer |> Seq.exactlyOne
    }
    static member LoadAsync (path: string) = task {
        let! idxFile = IdxFile.LoadAsync path
        return idxFile :?> IdxLabelFile
    }
    static member LoadAsync (stream: Stream) = task {
        let! idxFile = IdxFile.LoadAsync stream
        return idxFile :?> IdxLabelFile
    }
