// Copyright Y56380X https://github.com/Y56380X/IdxFileLoader.
// Licensed under the MIT License.

using CommunityToolkit.HighPerformance;
using IdxFileLoader;

await using var idxFile1 = await IdxFile.LoadAsync ("./train-images-idx3-ubyte/train-images.idx3-ubyte");
await using var idxFile2 = await IdxFile.LoadAsync ("./train-labels-idx1-ubyte/train-labels.idx1-ubyte");

Console.WriteLine($"File 1 is {idxFile1.GetType().Name}");
Console.WriteLine($"File 2 is {idxFile2.GetType().Name}");
Console.WriteLine();

if (idxFile1 is IdxImageFile imageFile)
{
    Console.WriteLine($"File 1 contains {imageFile.ImageCount} images ({imageFile.ImageSize})");
    
    var extractCount = imageFile.ImageCount < 20 ? imageFile.ImageCount : 20; 
    Console.WriteLine($"Extract {extractCount} images");
    
    var buffer = new byte[imageFile.ImageSize.Height, imageFile.ImageSize.Width];
    for (var i = 0; i < extractCount; i++)
    {
        await imageFile.ReadImageBufferAsync(buffer.AsMemory());
        var image = Image.LoadPixelData<L8>(buffer.AsSpan(), imageFile.ImageSize.Width, imageFile.ImageSize.Height);
        image.SaveAsPng($"./image_{i + 1}.png");
    }
    Console.WriteLine();
}

if (idxFile2 is IdxLabelFile labelFile)
{
    Console.WriteLine($"File 2 contains {labelFile.LabelCount} labels");
    
    var extractCount = labelFile.LabelCount < 20 ? labelFile.LabelCount : 20; 
    Console.WriteLine($"Extract {extractCount} labels");
    
    var labels = new byte[extractCount];
    for (var i = 0; i < extractCount; i++)
        labels[i] = await labelFile.ReadLabelAsync();
    Console.WriteLine(string.Join(", ", labels));
}
