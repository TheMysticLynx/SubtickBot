using CellEncoding;

namespace Indev2.Encoder
{
    public static class LevelEncoder
    {
        //First index is always the preferred when checking order
        public static readonly ILevelFormat[] SupportedFormats =
        {
            new ByteMash(),
            new LevelFormatB(),
            new LegacyFormatV3(),
        };

        public static DecodeResult Decode(string input)
        {
            foreach (var format in SupportedFormats)
               try
               {
                   if (!format.Matches(input))
                       continue;
                   return format.Decode(input);
               } catch (Exception)
               {
                   //
               }
            throw new Exception("Unknown format");
        }

        public static DecodeResult Decode(byte[] input)
        {
            foreach (var format in SupportedFormats)
            {
                if (!format.Matches(input))
                    continue;
                try
                {
                    return format.Decode(input);
                }
                catch (Exception)
                {
                    //
                }
            }

            throw new Exception("Unknown format");
        }
    }
}