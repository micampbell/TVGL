﻿// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 05-28-2016
// ***********************************************************************
// <copyright file="IOFunctions.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using TVGL.Voxelization;

namespace TVGL.IOFunctions
{
    /// <summary>
    ///     The IO or input/output class contains static functions for saving and loading files in common formats.
    ///     Note that as a Portable class library, these IO functions cannot interact with your file system. In order
    ///     to load or save, the filename is not enough. One needs to provide the stream.
    /// </summary>
    public abstract partial class IO
    {

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        internal string Name { get; set; }
        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        /// <value>
        /// The name of the file.
        /// </value>
        internal string FileName { get; set; }

        /// <summary>
        /// Gets or sets the unit.
        /// </summary>
        /// <value>The unit.</value>
        [XmlIgnore]
        public UnitType Units { get; set; }

        /// <summary>
        /// Gets or sets the units as string.
        /// </summary>
        /// <value>The units as string.</value>
        [XmlAttribute("unit")]
        public string UnitsAsString
        {
            get
            {
                return Enum.GetName(typeof(UnitType), Units);
            }
            set
            {
                Units = ParseUnits(value);
            }
        }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <value>The language.</value>
        [XmlAttribute("lang")]
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the comments.
        /// </summary>
        /// <value>
        /// The comments.
        /// </value>
        internal List<string> Comments => _comments;
        /// <summary>
        /// The _comments
        /// </summary>
        protected List<string> _comments = new List<string>();

        #region Open/Load/Read

        /// <summary>
        /// Opens the 3D solid or solids from a provided file name.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>
        /// A list of TessellatedSolids.
        /// </returns>
        /// <exception cref="FileNotFoundException">The file was not found at: " + filename</exception>
        public static void Open(string filename, out TessellatedSolid solid)
        {
            if (File.Exists(filename))
                using (var fileStream = File.OpenRead(filename))
                    Open(fileStream, filename, out solid);
            else throw new FileNotFoundException("The file was not found at: " + filename);
        }
        public static void Open(string filename, out TessellatedSolid[] solids)
        {
            if (File.Exists(filename))
                using (var fileStream = File.OpenRead(filename))
                    Open(fileStream, filename, out solids);
            else throw new FileNotFoundException("The file was not found at: " + filename);
        }
        public static void Open(string filename, out VoxelizedSolid solid)
        {
            if (File.Exists(filename))
                using (var fileStream = File.OpenRead(filename))
                    Open(fileStream, out solid);
            else throw new FileNotFoundException("The file was not found at: " + filename);

        }

        public static void Open(string filename, out CrossSectionSolid solid)
        {
            if (File.Exists(filename))
                using (var fileStream = File.OpenRead(filename))
                    Open(fileStream, out solid);
            else throw new FileNotFoundException("The file was not found at: " + filename);

        }
        /// <summary>
        /// Opens the specified stream, s. Note that as a Portable class library
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="solid">The solid.</param>
        /// <returns>TessellatedSolid.</returns>
        /// <exception cref="Exception">Cannot open file without extension (e.g. f00.stl).
        /// or
        /// This function has been recently removed.
        /// or
        /// Cannot determine format from extension (not .stl, .ply, .3ds, .lwo, .obj, .objx, or .off.</exception>
        /// <exception cref="System.Exception">Cannot open file without extension (e.g. f00.stl).
        /// or
        /// This function has been recently removed.
        /// or
        /// Cannot determine format from extension (not .stl, .ply, .3ds, .lwo, .obj, .objx, or .off.</exception>
        public static void Open(Stream s, string filename, out TessellatedSolid solid)
        {
            try
            {
                var extension = GetFileTypeFromExtension(Path.GetExtension(filename));
                switch (extension)
                {
                    case FileType.STL_ASCII:
                    case FileType.STL_Binary:
                        solid = STLFileData.OpenSolids(s, filename)[0]; // Standard Tessellation or StereoLithography
                        break;
                    case FileType.ThreeMF:
                        solid = ThreeMFFileData.OpenSolids(s, filename)[0];
                        break;
                    case FileType.Model3MF:
                        solid = ThreeMFFileData.OpenModelFile(s, filename)[0];
                        break;
                    case FileType.AMF:
                        solid = AMFFileData.OpenSolids(s, filename)[0];
                        break;
                    case FileType.OFF:
                        solid = OFFFileData.OpenSolid(s, filename);
                        // http://en.wikipedia.org/wiki/OFF_(file_format)
                        break;
                    case FileType.PLY_ASCII:
                    case FileType.PLY_Binary:
                        solid = PLYFileData.OpenSolid(s, filename);
                        break;
                    case FileType.SHELL:
                        solid = ShellFileData.OpenSolids(s, filename)[0];
                        break;
                    case FileType.STEP:
                        solid = STEPFileData.OpenSolids(s, filename)[0];
                        break;
                    default:
                        var serializer = new JsonSerializer();
                        var sr = new StreamReader(s);
                        using (var reader = new JsonTextReader(sr))
                            solid = serializer.Deserialize<TessellatedSolid>(reader);
                        break;
                }
            }
            catch (Exception exc)
            {
                throw new Exception("Cannot open file. Message: " + exc.Message);
            }
        }

        public static void Open(Stream s, string filename, out TessellatedSolid[] tessellatedSolids)
        {
            try
            {
                var extension = GetFileTypeFromExtension(Path.GetExtension(filename));
                switch (extension)
                {
                    case FileType.STL_ASCII:
                    case FileType.STL_Binary:
                        tessellatedSolids = STLFileData.OpenSolids(s, filename); // Standard Tessellation or StereoLithography
                        break;
                    case FileType.ThreeMF:
                        tessellatedSolids = ThreeMFFileData.OpenSolids(s, filename);
                        break;
                    case FileType.Model3MF:
                        tessellatedSolids = ThreeMFFileData.OpenModelFile(s, filename);
                        break;
                    case FileType.AMF:
                        tessellatedSolids = AMFFileData.OpenSolids(s, filename);
                        break;
                    case FileType.SHELL:
                        tessellatedSolids = ShellFileData.OpenSolids(s, filename);
                        break;
                    case FileType.OFF:
                    case FileType.PLY_ASCII:
                    case FileType.PLY_Binary:
                        throw new Exception("Attempting to open multiple solids with a " + extension.ToString() + " file.");
                    default:
                        var serializer = new JsonSerializer();
                        var sr = new StreamReader(s);
                        using (var reader = new JsonTextReader(sr))
                            // note this is a hack...<T> is overly specific
                            tessellatedSolids = serializer.Deserialize<TessellatedSolid[]>(reader);
                        break;
                }
            }
            catch (Exception exc)
            {
                throw new Exception("Cannot open file. Message: " + exc.Message);
            }
        }
        public static void Open(Stream s, out VoxelizedSolid solid)
        {
            var serializer = new JsonSerializer();
            var sr = new StreamReader(s);
            using (var reader = new JsonTextReader(sr))
                solid = serializer.Deserialize<VoxelizedSolid>(reader);
        }
        public static void Open(Stream s, out CrossSectionSolid solid)
        {
            var serializer = new JsonSerializer();
            var sr = new StreamReader(s);
            using (var reader = new JsonTextReader(sr))
                solid = serializer.Deserialize<CrossSectionSolid>(reader);
        }
        public static Solid Open(string filename)
        {
            if (File.Exists(filename))
                using (var fileStream = File.OpenRead(filename))
                    return Open(fileStream, filename);
            else throw new FileNotFoundException("The file was not found at: " + filename);
        }
        public static Solid Open(Stream s, string filename = "")
        {
#if !DEBUG
            try
            {
#endif
                var extension = GetFileTypeFromExtension(Path.GetExtension(filename));
                switch (extension)
                {
                    case FileType.STL_ASCII:
                    case FileType.STL_Binary:
                        return STLFileData.OpenSolids(s, filename)[0]; // Standard Tessellation or StereoLithography
                    case FileType.ThreeMF:
                        return ThreeMFFileData.OpenSolids(s, filename)[0];
                    case FileType.Model3MF:
                        return ThreeMFFileData.OpenModelFile(s, filename)[0];
                    case FileType.AMF:
                        return AMFFileData.OpenSolids(s, filename)[0];
                    case FileType.OFF:
                        return OFFFileData.OpenSolid(s, filename);
                    case FileType.PLY_ASCII:
                    case FileType.PLY_Binary:
                        return PLYFileData.OpenSolid(s, filename);
                    case FileType.SHELL:
                        return ShellFileData.OpenSolids(s, filename)[0];
                    case FileType.STEP:
                        return STEPFileData.OpenSolids(s, filename)[0];
                    default:
                        var serializer = new JsonSerializer();
                        var sr = new StreamReader(s);
                        using (var reader = new JsonTextReader(sr))
                        {
                            JObject jObject = JObject.Load(reader);
                            var typeString = ((string)jObject["TVGLSolidType"]);
                            if (string.IsNullOrWhiteSpace(typeString)) return null;
                            var type = Type.GetType(typeString);
                            if (type == null)
                            {
                                var assembly = Assembly.LoadFrom((string)jObject["InAssembly"]);
                                type = assembly.GetType(typeString);
                            }
                            if (type == null) return null;
                            return (Solid)JsonConvert.DeserializeObject(jObject.ToString(), type);
                        }
                }
#if !DEBUG
        }
            catch (Exception exc)
            {
                throw new Exception("Cannot open file. Message: " + exc.Message);
            }
#endif
        }

        public static void OpenFromString(string data, FileType fileType, out TessellatedSolid solid)
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(data);
                writer.Flush();
            }
            stream.Position = 0;
            var name = "data." + GetExtensionFromFileType(fileType);
            Open(stream, name, out solid);
        }
        public static void OpenFromString(string data, FileType fileType, out TessellatedSolid[] solids)
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(data);
                writer.Flush();
            }
            stream.Position = 0;
            var name = "data." + GetExtensionFromFileType(fileType);
            Open(stream, name, out solids);
        }
        public static void OpenFromString(string data, out VoxelizedSolid solid)
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(data);
                writer.Flush();
            }
            stream.Position = 0;
            Open(stream, out solid);
        }
        public static void OpenFromString(string data, out CrossSectionSolid solid)
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(data);
                writer.Flush();
            }
            stream.Position = 0;
            Open(stream, out solid);
        }

        private static FileType GetFileTypeFromExtension(string extension)
        {
            extension = extension.ToLower(CultureInfo.InvariantCulture).Trim(' ', '.');
            switch (extension)
            {
                case "stl": return FileType.STL_ASCII;
                case "3mf": return FileType.ThreeMF;
                case "model": return FileType.Model3MF;
                case "amf": return FileType.AMF;
                case "off": return FileType.OFF;
                case "ply": return FileType.PLY_ASCII;
                case "shell": return FileType.SHELL;
                case "stp":
                case "step": return FileType.STEP;
                case "tvgl":
                case "json": return FileType.TVGL;
                default: return FileType.unspecified;
            }
        }

        private static string GetExtensionFromFileType(FileType fileType)
        {
            switch (fileType)
            {
                case FileType.STL_ASCII:
                case FileType.STL_Binary: return "stl";
                case FileType.ThreeMF: return "3mf";
                case FileType.Model3MF: return "model";
                case FileType.AMF: return "amf";
                case FileType.OFF: return "off";
                case FileType.PLY_ASCII:
                case FileType.PLY_Binary: return "ply";
                case FileType.SHELL: return "shell";
                case FileType.STEP: return "stp";
                case FileType.TVGL: return "tvgl";
                default: return "";
            }
        }
        /// <summary>
        ///     Parses the ID and values from the specified line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="id">The id.</param>
        /// <param name="values">The values.</param>
        protected static void ParseLine(string line, out string id, out string values)
        {
            line = line.Trim();
            var idx = line.IndexOf(' ');
            if (idx == -1)
            {
                id = line;
                values = String.Empty;
            }
            else
            {
                id = line.Substring(0, idx).ToLower(CultureInfo.InvariantCulture);
                values = line.Substring(idx + 1);
            }
        }


        /// <summary>
        ///     Tries to parse a vertex from a string.
        /// </summary>
        /// <param name="line">The input string.</param>
        /// <param name="doubles">The vertex point.</param>
        /// <returns>True if parsing was successful.</returns>
        protected static bool TryParseDoubleArray(string line, out double[] doubles)
        {
            var strings = line.Split(' ', '\t').ToList();
            strings.RemoveAll(String.IsNullOrWhiteSpace);
            doubles = new double[strings.Count];
            for (var i = 0; i < strings.Count; i++)
            {
                if (!Double.TryParse(strings[i], out doubles[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        ///     Tries to parse a vertex from a string.
        /// </summary>
        /// <param name="parser">The parser.</param>
        /// <param name="line">The input string.</param>
        /// <param name="doubles">The vertex point.</param>
        /// <returns>True if parsing was successful.</returns>
        protected static bool TryParseDoubleArray(Regex parser, string line, out double[] doubles)
        {
            var match = parser.Match(line);
            if (!match.Success)
            {
                doubles = null;
                return false;
            }
            doubles = new double[match.Groups.Count - 1];
            for (var i = 0; i < doubles.GetLength(0); i++)
            {
                if (!Double.TryParse(match.Groups[i + 1].Value, out doubles[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        ///     Reads the next line with the expectation that is starts with the
        ///     "expected" string. If the line does not have the expected value,
        ///     then the StreamReader stays at the same position and
        ///     false is returned.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="expected">The expected.</param>
        /// <returns>bool.</returns>
        protected static bool ReadExpectedLine(StreamReader reader, string expected)
        {
            var startPosition = reader.BaseStream.Position;
            var line = ReadLine(reader);
            if (line != null && expected.Equals(line.Trim(' '), StringComparison.CurrentCultureIgnoreCase))
                return true;
            reader.BaseStream.Position = startPosition;
            return false;
        }

        /// <summary>
        ///     Reads the line.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>System.String.</returns>
        protected static string ReadLine(StreamReader reader)
        {
            string line;
            do
            {
                line = reader.ReadLine();
                if (reader.EndOfStream) break;
            } while (String.IsNullOrWhiteSpace(line));
            return line.Trim();
        }

        /// <summary>
        /// Infers the units from comments.
        /// </summary>
        /// <param name="comments">The comments.</param>
        /// <returns></returns>
        protected static UnitType InferUnitsFromComments(List<string> comments)
        {
            foreach (var comment in comments)
            {
                var words = Regex.Matches(comment, "([a-z]+)");
                foreach (var word in words)
                    if (TryParseUnits(word.ToString(), out UnitType units)) return units;
            }
            return UnitType.unspecified;
        }

        /// <summary>
        /// Tries to parse units.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="units">The units.</param>
        /// <returns></returns>
        protected static bool TryParseUnits(string input, out UnitType units)
        {
            if (Enum.TryParse(input, out units)) return true;
            if (input.Equals("milimeter", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("millimeter", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("milimeters", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("millimeters", StringComparison.CurrentCultureIgnoreCase))
                units = UnitType.millimeter;
            else if (input.Equals("micrometer", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("micrometers", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("microns", StringComparison.CurrentCultureIgnoreCase))
                units = UnitType.micron;
            else if (input.Equals("centimeter", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("centimeters", StringComparison.CurrentCultureIgnoreCase))
                units = UnitType.centimeter;
            else if (input.Equals("meters", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("meter", StringComparison.CurrentCultureIgnoreCase))
                units = UnitType.meter;
            else if (input.Equals("feet", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("foots", StringComparison.CurrentCultureIgnoreCase))
                units = UnitType.foot;
            else if (input.Equals("inches", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("inch", StringComparison.CurrentCultureIgnoreCase))
                units = UnitType.inch;
            else return false;
            return true;
        }

        /// <summary>
        /// Parses the units.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>TVGL.UnitType.</returns>
        protected static UnitType ParseUnits(string input)
        {
            if (TryParseUnits(input, out UnitType units)) return units;
            return UnitType.unspecified;
        }


        internal static int ReadNumberAsInt(BinaryReader reader, Type type, FormatEndiannessType formatType)
        {
            var bigEndian = (formatType == FormatEndiannessType.binary_big_endian);

            if (type == typeof(double))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return (int)Math.Round(BitConverter.ToDouble(byteArray, 0));
            }
            if (type == typeof(long))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return (int)BitConverter.ToInt64(byteArray, 0);
            }
            if (type == typeof(ulong))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return (int)BitConverter.ToUInt64(byteArray, 0);
            }
            if (type == typeof(float))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return (int)Math.Round(BitConverter.ToSingle(byteArray, 0));
            }
            if (type == typeof(int))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToInt32(byteArray, 0);
            }
            if (type == typeof(uint))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return (int)BitConverter.ToUInt32(byteArray, 0);
            }
            if (type == typeof(short))
            {
                var byteArray = reader.ReadBytes(2);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToInt16(byteArray, 0);
            }
            if (type == typeof(ushort))
            {
                var byteArray = reader.ReadBytes(2);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToUInt16(byteArray, 0);
            }
            if (type == typeof(byte))
            {
                var oneByteArray = reader.ReadBytes(1);
                return oneByteArray[0];
            }
            return int.MinValue;
        }
        internal static float ReadNumberAsFloat(BinaryReader reader, Type type, FormatEndiannessType formatType)
        {
            var bigEndian = (formatType == FormatEndiannessType.binary_big_endian);

            if (type == typeof(double))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return (float)BitConverter.ToDouble(byteArray, 0);
            }
            if (type == typeof(long))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToInt64(byteArray, 0);
            }
            if (type == typeof(ulong))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToUInt64(byteArray, 0);
            }
            if (type == typeof(float))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToSingle(byteArray, 0);
            }
            if (type == typeof(int))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToInt32(byteArray, 0);
            }
            if (type == typeof(uint))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToUInt32(byteArray, 0);
            }
            if (type == typeof(short))
            {
                var byteArray = reader.ReadBytes(2);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToInt16(byteArray, 0);
            }
            if (type == typeof(ushort))
            {
                var byteArray = reader.ReadBytes(2);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToUInt16(byteArray, 0);
            }
            if (type == typeof(byte))
            {
                var oneByteArray = reader.ReadBytes(1);
                return oneByteArray[0];
            }
            return float.NaN;
        }
        internal static double ReadNumberAsDouble(BinaryReader reader, Type type, FormatEndiannessType formatType)
        {
            var bigEndian = (formatType == FormatEndiannessType.binary_big_endian);

            if (type == typeof(double))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToDouble(byteArray, 0);
            }
            if (type == typeof(long))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToInt64(byteArray, 0);
            }
            if (type == typeof(ulong))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToUInt64(byteArray, 0);
            }
            if (type == typeof(float))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToSingle(byteArray, 0);
            }
            if (type == typeof(int))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToInt32(byteArray, 0);
            }
            if (type == typeof(uint))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToUInt32(byteArray, 0);
            }
            if (type == typeof(short))
            {
                var byteArray = reader.ReadBytes(2);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToInt16(byteArray, 0);
            }
            if (type == typeof(ushort))
            {
                var byteArray = reader.ReadBytes(2);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToUInt16(byteArray, 0);
            }
            if (type == typeof(byte))
            {
                var oneByteArray = reader.ReadBytes(1);
                return oneByteArray[0];
            }
            return double.NaN;
        }

        // These methods are currently not used, but it seems that encoding a doubles array as
        // a binary char array would be better than parsing the text. There would be 1) less
        // chance of roundoff,quicker conversions, and - in most cases- a smaller file.
        // however, experiment in early Jan2020, did not tend to show this. but even the 
        // doubles changed value which makes me think I didn't do a good job with it. 
        // Come back to this in the future?
        internal static string ConvertDoubleArrayToString(IEnumerable<double> doubles)
        {
            var byteArray = doubles.SelectMany(x => BitConverter.GetBytes(x)).ToArray();
            return System.Text.Encoding.Unicode.GetString(byteArray);
        }
        internal static double[] ConvertStringToDoubleArray(string doublesAsString)
        {
            var bytes = System.Text.Encoding.Unicode.GetBytes(doublesAsString);
            double[] values = new double[bytes.Length / 8];
            for (int i = 0; i < values.Length; i++)
                values[i] = BitConverter.ToDouble(bytes, i * 8);
            return values;
        }

        /// <summary>
        /// Tries the parse number type from string.
        /// </summary>
        /// <param name="typeString">The type string.</param>
        /// <param name="type">The type.</param>
        /// <returns>Type.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected static bool TryParseNumberTypeFromString(string typeString, out Type type)
        {
            if (typeString.StartsWith("float", StringComparison.CurrentCultureIgnoreCase)
                || typeString.StartsWith("single", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(float);
            else if (typeString.StartsWith("double", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(double);
            else if (typeString.StartsWith("long", StringComparison.CurrentCultureIgnoreCase)
                || typeString.StartsWith("int64", StringComparison.CurrentCultureIgnoreCase)
                || typeString.StartsWith("uint64", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(long);
            else if (typeString.StartsWith("int32", StringComparison.CurrentCultureIgnoreCase)
                     || typeString.Equals("int", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(int);
            else if (typeString.StartsWith("short", StringComparison.CurrentCultureIgnoreCase)
                     || typeString.StartsWith("int16", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(short);
            else if (typeString.StartsWith("ushort", StringComparison.CurrentCultureIgnoreCase)
                || typeString.StartsWith("uint16", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(ushort);
            else if (typeString.StartsWith("uint64", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(ulong);
            else if (typeString.Equals("uint", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(uint);
            else if (typeString.StartsWith("char", StringComparison.CurrentCultureIgnoreCase)
                     || typeString.StartsWith("uchar", StringComparison.CurrentCultureIgnoreCase)
                     || typeString.StartsWith("byte", StringComparison.CurrentCultureIgnoreCase)
                     || typeString.StartsWith("uint8", StringComparison.CurrentCultureIgnoreCase)
                     || typeString.StartsWith("int8", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(byte);
            else
            {
                type = null;
                return false;
            }
            return true;
        }

        internal static int ReadNumberAsInt(string text, Type type)
        {
            if (type == typeof(double))
            {
                if (double.TryParse(text, out var value)) return (int)Math.Round(value);
            }
            if (type == typeof(long))
            {
                if (long.TryParse(text, out var value)) return (int)value;
            }
            if (type == typeof(ulong))
            {
                if (ulong.TryParse(text, out var value)) return (int)value;
            }
            if (type == typeof(float))
            {
                if (float.TryParse(text, out var value)) return (int)Math.Round(value);
            }
            if (type == typeof(int))
            {
                if (int.TryParse(text, out var value)) return value;
            }
            if (type == typeof(uint))
            {
                if (uint.TryParse(text, out var value)) return (int)value;
            }
            if (type == typeof(short))
            {
                if (short.TryParse(text, out var value)) return value;
            }
            if (type == typeof(ushort))
            {
                if (ushort.TryParse(text, out var value)) return value;
            }
            if (type == typeof(byte))
            {
                if (byte.TryParse(text, out var value)) return value;
            }
            return int.MinValue;
        }
        internal static float ReadNumberAsFloat(string text, Type type)
        {
            if (type == typeof(double))
            {
                if (double.TryParse(text, out var value)) return (float)value;
            }
            if (type == typeof(long))
            {
                if (long.TryParse(text, out var value)) return value;
            }
            if (type == typeof(ulong))
            {
                if (ulong.TryParse(text, out var value)) return value;
            }
            if (type == typeof(float))
            {
                if (float.TryParse(text, out var value)) return value;
            }
            if (type == typeof(int))
            {
                if (int.TryParse(text, out var value)) return value;
            }
            if (type == typeof(uint))
            {
                if (uint.TryParse(text, out var value)) return value;
            }
            if (type == typeof(short))
            {
                if (short.TryParse(text, out var value)) return value;
            }
            if (type == typeof(ushort))
            {
                if (ushort.TryParse(text, out var value)) return value;
            }
            if (type == typeof(byte))
            {
                if (byte.TryParse(text, out var value)) return value;
            }
            return float.NaN;
        }
        internal static double ReadNumberAsDouble(string text, Type type)
        {
            if (type == typeof(double))
            {
                if (Double.TryParse(text, out var value)) return value;
            }
            if (type == typeof(long))
            {
                if (Int64.TryParse(text, out var value)) return value;
            }
            if (type == typeof(ulong))
            {
                if (UInt64.TryParse(text, out var value)) return value;
            }
            if (type == typeof(float))
            {
                if (Single.TryParse(text, out var value)) return value;
            }
            if (type == typeof(int))
            {
                if (Int32.TryParse(text, out var value)) return value;
            }
            if (type == typeof(uint))
            {
                if (UInt32.TryParse(text, out var value)) return value;
            }
            if (type == typeof(short))
            {
                if (Int16.TryParse(text, out var value)) return value;
            }
            if (type == typeof(ushort))
            {
                if (UInt16.TryParse(text, out var value)) return value;
            }
            if (type == typeof(byte))
            {
                if (Byte.TryParse(text, out var value)) return value;
            }
            return Double.NaN;
        }
#endregion

#region Save/Write
        /// <summary>
        /// Saves the specified solids to a file.
        /// </summary>
        /// <param name="solids">The solids.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns></returns>
        public static bool Save(IList<Solid> solids, string filename, FileType fileType = FileType.unspecified)
        {
            if (fileType == FileType.unspecified)
                fileType = GetFileTypeFromExtension(Path.GetExtension(filename));
            filename = Path.GetFileNameWithoutExtension(filename) + "." + GetExtensionFromFileType(fileType);
            using (var fileStream = File.OpenWrite(filename))
                return Save(fileStream, solids, fileType);
        }
        /// <summary>
        /// Saves the specified solid to a file.
        /// </summary>
        /// <param name="solid">The solid.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns></returns>
        public static bool Save(Solid solid, string filename, FileType fileType = FileType.unspecified)
        {
            if (fileType == FileType.unspecified)
                fileType = GetFileTypeFromExtension(Path.GetExtension(filename));
            filename = Path.GetDirectoryName(filename)+Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(filename) 
                + "." + GetExtensionFromFileType(fileType);
            using (var fileStream = File.OpenWrite(filename))
                return Save(fileStream, solid, fileType);
        }
        /// <summary>
        ///     Saves the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solids">The solids.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool Save(Stream stream, IList<Solid> solids, FileType fileType = FileType.TVGL)
        {
            if (solids.Count == 0) return false;
            if (solids.Count == 1) return Save(stream, solids[0], fileType);
            switch (fileType)
            {
                case FileType.STL_ASCII:
                    return STLFileData.SaveASCII(stream, solids.Cast<TessellatedSolid>().ToArray());
                case FileType.STL_Binary:
                    return STLFileData.SaveBinary(stream, solids.Cast<TessellatedSolid>().ToArray());
                case FileType.AMF:
                    return AMFFileData.SaveSolids(stream, solids.Cast<TessellatedSolid>().ToArray());
                case FileType.ThreeMF:
                    return ThreeMFFileData.Save(stream, solids.Cast<TessellatedSolid>().ToArray());
                case FileType.Model3MF:
                    return ThreeMFFileData.SaveModel(stream, solids.Cast<TessellatedSolid>().ToArray());
                case FileType.OFF:
                    throw new NotSupportedException(
                        "The OFF format does not support saving multiple solids to a single file.");
                case FileType.PLY_ASCII:
                    throw new NotSupportedException(
                        "The PLY format does not support saving multiple solids to a single file.");
                case FileType.PLY_Binary:
                    throw new NotSupportedException(
                        "The PLY format does not support saving multiple solids to a single file.");
                default:
                    solids.Select(solid => Save(stream, solid, fileType));
                    return true;
            }
        }


        /// <summary>
        /// Saves the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solid">The solid.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns>System.Boolean.</returns>
        public static bool Save(Stream stream, Solid solid, FileType fileType = FileType.TVGL)
        {
            switch (fileType)
            {
                case FileType.STL_ASCII:
                    return STLFileData.SaveASCII(stream, new[] { (TessellatedSolid)solid });
                case FileType.STL_Binary:
                    return STLFileData.SaveBinary(stream, new[] { (TessellatedSolid)solid });
                case FileType.AMF:
                    return AMFFileData.SaveSolids(stream, new[] { (TessellatedSolid)solid });
                case FileType.ThreeMF:
                    return ThreeMFFileData.Save(stream, new[] { (TessellatedSolid)solid });
                case FileType.Model3MF:
                    return ThreeMFFileData.SaveModel(stream, new[] { (TessellatedSolid)solid });
                case FileType.OFF:
                    return OFFFileData.SaveSolid(stream, (TessellatedSolid)solid);
                case FileType.PLY_ASCII:
                    return PLYFileData.SaveSolidASCII(stream, (TessellatedSolid)solid);
                case FileType.PLY_Binary:
                    return PLYFileData.SaveSolidBinary(stream, (TessellatedSolid)solid);
                case FileType.SHELL:
                    return ShellFileData.Save(stream, (TessellatedSolid)solid);
                default:
                    JsonSerializer serializer = new JsonSerializer
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    };
                    var sw = new StreamWriter(stream);
                    using (var writer = new JsonTextWriter(sw))
                    {
                        var jObject = JObject.FromObject(solid, serializer);
                        var solidType = solid.GetType();
                        jObject.AddFirst(new JProperty("TVGLSolidType", solid.GetType().FullName));
                        if (!Assembly.GetExecutingAssembly().Equals(solidType.Assembly))
                            jObject.AddFirst(new JProperty("InAssembly", solidType.Assembly.Location));
                        jObject.WriteTo(writer);
                    }
                    return true;
            }
        }


        /// <summary>
        /// Saves the solid as a string.
        /// </summary>
        /// <param name="solid">The solid.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns>System.String.</returns>
        public static string SaveToString(Solid solid, FileType fileType = FileType.unspecified)
        {
            using (var stream = new MemoryStream())
            {
                if (!Save(stream, solid, fileType)) return "";
                var byteArray = stream.ToArray();
                return System.Text.Encoding.Unicode.GetString(byteArray, 0, byteArray.Length);
            }
        }

        /// <summary>
        /// Saves the solids as a string.
        /// </summary>
        /// <param name="solids">The solids.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns>System.String.</returns>
        public static string SaveToString(IList<Solid> solids, FileType fileType = FileType.unspecified)
        {
            using (var stream = new MemoryStream())
            {
                if (!Save(stream, solids, fileType)) return "";
                var byteArray = stream.ToArray();
                return System.Text.Encoding.Unicode.GetString(byteArray, 0, byteArray.Length);
            }
        }


        /// <summary>
        /// Gets the TVGL date mark text.
        /// </summary>
        /// <value>The TVGL date mark text.</value>
        protected static string TvglDateMarkText
        {
            get
            {
                var now = DateTime.Now;
                return "created by TVGL on " + now.Year + "/" + now.Month + "/" + now.Day + " at " + now.Hour + ":" +
                       now.Minute + ":" + now.Second;
            }
        }
#endregion
    }
}