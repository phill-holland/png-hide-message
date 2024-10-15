# PNG Hidden Message Demonstration

This is a simple dotnet core console application demonstration encoding simple messages within images (png files), this differs slightly from the steganography demonstration here;

https://github.com/phill-holland/steganography-example

# Method

Every PNG file compresses image data using the ZLib compression format, typically the length of the data is only stored for the compressed data, rather than what the uncompressed size of the data should be, this is usually calculated when loading in an image viewer application, width * height.

You can exploit this by appending a message onto the end of the compressed data and the image viewer application will just ignore the extra data.

# Requirements

- dotnet core SDK 8 or above installed

# Running

at the command prompt, type;

```
dotnet run
```

A provided test image is already included images/cake.png and the output will be in images/output.png


