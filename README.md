# IdxFileLoader
Loading idx files in .NET from datasets like MNIST.

## Samples

One example for loading idx files and retrieving the images and labels is in the `samples` subfolder.

Load first image to 2-D byte buffer:
```csharp
await using var idxImageFile = await IdxImageFile.LoadAsync("./train-images.idx3-ubyte");
var buffer = await idxImageFile.ReadImageBufferAsync();
```

Load first label as byte value:
```csharp
await using var idxLabelFile = await IdxLabelFile.LoadAsync("./train-labels.idx1-ubyte");
var label = await idxLabelFile.ReadLabelAsync();
```

Open GZip files directly is also possible:
```csharp
await using var idxImageFile = await IdxImageFile.LoadAsync(
    new GZipStream(File.OpenRead("./train-images-idx3-ubyte.gz"), CompressionMode.Decompress));
await using var idxLabelFile = await IdxLabelFile.LoadAsync(
    new GZipStream(File.OpenRead("./train-labels-idx1-ubyte.gz"), CompressionMode.Decompress));

Console.WriteLine($"Image Count: {idxImageFile.ImageCount}");
Console.WriteLine($"Label Count: {idxLabelFile.LabelCount}");
```
