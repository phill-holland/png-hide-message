# Steganography Demonstration

This is a simple dotnet core console application demonstration encoding simple messages within images (png files)

# Method

A 32 bit colour png image is loaded, and then within the output the first pixel is then used to encode the size of the message;

```
    UInt32 temp = (UInt32)message.Length;
    
    pixel.R = (byte)(temp & 0xFF);
    pixel.G = (byte)((temp >> 8) & 0xFF);
    pixel.B = (byte)((temp >> 16) & 0xFF);
    pixel.A = (byte)((temp >> 24) & 0xFF);
```

The size can be any 32 bit unsigned integer, which then using bit shifting, is split up and divided between the four colours of the pixel (red,green,blue and alpha)

The message is then converted to ASCII values, and each ASCII byte value, is spread across the lowest two bits of each colour of the destination pixel.

For example;

```
    ASCII letter a = '97' (decimal)

    ASCII letter a = 0110 0001 (binary)

    AA  BB  GG  RR
    01  10  00  01   

    red = 01
    green = 00
    blue = 10
    alpha = 01

    new_red = XX XX XX 01
    new_green = XX XX XX 00
    new_blue = XX XX XX 10
    new_alpha = XX XX XX 01

    (where XX represents the exist colour bit values already present in the source image)
```

Code extract;

```
    pixel.R = (byte)(pixel.R | (value & (0x3)));
    pixel.G = (byte)(pixel.G | (value >> 2 & (0x3)));
    pixel.B = (byte)(pixel.B | (value >> 4 & (0x3)));
    pixel.A = (byte)(pixel.A | (value >> 6 & (0x3)));
```

Using this method reduces any chance of significantly changing the colour of any given pixel, the change should not be perceivable by the naked eye.

# Requirements

- dotnet core SDK 6 or above installed

# Running

at the command prompt, type;

```
dotnet run
```



