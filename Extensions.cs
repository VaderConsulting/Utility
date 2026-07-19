using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Utility
{
    public static class Extensions
    {
        #region Constants 

        private const long OneKb = 1024;
        private const long OneMb = OneKb * 1024;
        private const long OneGb = OneMb * 1024;
        private const long OneTb = OneGb * 1024;

        #endregion

        #region Events 

        #endregion

        #region Enums 

        #endregion

        #region DLL Imports 


        #endregion

        #region Fields 

        #endregion

        #region Properties 

        #endregion

        #region Constructors 

        #endregion

        #region Event Handlers 

        #endregion

        #region Private Methods 



        #endregion

        #region Public Methods 

        #region T

        /// <summary>
        /// Perform a binary copy of the provided object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item">The object to copy</param>
        /// <returns></returns>
        public static T DeepCopy<T>(this T item)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();

            try
            {
                formatter.Serialize(stream, item);
                stream.Seek(0, SeekOrigin.Begin);
                T Result = (T)formatter.Deserialize(stream);
                stream.Close();

                return Result;
            }
            catch (SerializationException)
            {
            }
            catch (Exception)
            {
                return item;
            }

            return item;
        }

        /// <summary>
        /// Deserialise to the provided type from the provided Filename
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Value"></param>
        /// <param name="Filename">The filename to load from</param>
        /// <param name="MaxWaitTimemS">The maximum time to wait for the provided file to unlock</param>
        /// <returns></returns>
        public static T LoadFromFile<T>(this T Value, string Filename, int MaxWaitTimemS = 60000)
        {
            T Result = default(T);
            string Flagfile = Filename + ".Flag";
            DateTime MaxTimeStamp = DateTime.UtcNow.AddMilliseconds(MaxWaitTimemS); // Use UtcNow to prevent DaylightSavings issues 

            if (Filename.LengthOf() == 0)
            {
                return default(T);
            }

            // Wait a maximum of MaxWaitTimemS for the Flag file to be deleted 
            while (System.IO.File.Exists(Flagfile) || DateTime.UtcNow > MaxTimeStamp)
            {
                System.Threading.Thread.Sleep(1); // Highly unlikely that we can wait for 1mS (due to the Timer resolution), 
                                                  // but we will use that as our minimum time to decrease apparent CPU utilisation 
            }

            // If the Flagfile still exists, return a negative result 
            if (System.IO.File.Exists(Flagfile))
            {
                return default(T);
            }

            try
            {
                using (FileStream lockFile = new FileStream(Flagfile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Delete))
                {
                    using (FileStream Stream = System.IO.File.OpenRead(Filename))
                    {
                        XmlSerializer Serializer = new XmlSerializer(typeof(T));

                        Result = (T)Serializer.Deserialize(Stream);
                    }

                    // We are finished with the Flag, so remove it 
                    System.IO.File.Delete(Flagfile);
                }
            }
            catch (Exception)
            {
                return default(T);
            }

            return Result;
        }

        /// <summary>
        /// Perform a copy of the provided object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Value"></param>
        /// <param name="Deep">If true (default), performs a DeepCopy</param>
        /// <returns><see cref="string"/> version of the provided object</returns>
        public static string Save<T>(this T Value, bool Deep = true)
        {
            XmlSerializer Serialiser = null;

            try
            {
                Serialiser = new XmlSerializer(typeof(T));
                using (StringWriter Writer = new StringWriter())
                {
                    if (Deep)
                    {
                        Serialiser.Serialize(Writer, Value.DeepCopy());
                    }
                    else
                    {
                        Serialiser.Serialize(Writer, Value);
                    }

                    return Writer.ToString();
                }

            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Serialise the target object to the provided Filename
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Value"></param>
        /// <param name="Filename">The filename to save to</param>
        /// <param name="Deep">If true (default), performs a DeepCopy</param>
        /// <param name="MaxWaitTimemS">The maximum time to wait for the provided file to unlock</param>
        /// <returns><see cref="bool"/> indicating success (true) or failure (false)</returns>
        public static bool SaveToFile<T>(this T Value, string Filename, bool Deep = true, int MaxWaitTimemS = 60000)
        {
            bool Result = false;
            string Flagfile = Filename + ".Flag";
            DateTime MaxTimeStamp = DateTime.UtcNow.AddMilliseconds(MaxWaitTimemS); // Use UtcNow to prevent DaylightSavings issues 

            // Wait a maximum of MaxWaitTimemS for the Flag file to be deleted 
            while (System.IO.File.Exists(Flagfile) || DateTime.UtcNow > MaxTimeStamp)
            {
                System.Threading.Thread.Sleep(1); // Highly unlikely that we can wait for 1mS (due to the Timer resolution), 
                                                  // but we will use that as our minimum time to decrease apparent CPU utilisation 
            }

            // If the Flagfile still exists, return a negative result 
            if (System.IO.File.Exists(Flagfile))
            {
                return false;
            }

            try
            {
                using (FileStream lockFile = new FileStream(Flagfile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Delete))
                {
                    FileStream fs = new FileStream(Filename, FileMode.Create, FileAccess.Write, FileShare.None);
                    XmlSerializer Serializer = new XmlSerializer(typeof(T));
                    TextWriter writer = null;

                    // Create String representation of the object 
                    string SerialisedObject = Value.Save(Deep);

                    // Write this to disk 
                    using (writer = new StreamWriter(fs))
                    {
                        Serializer.Serialize(writer, Value);
                        writer.Flush();
                        writer.Close();
                    }

                    // We are finished with the Flag, so remove it 
                    System.IO.File.Delete(Flagfile);

                    Result = true;
                }
            }
            catch
            {
                Result = false;
            }

            return Result;
        }

        //public static T Load<T>(this T obj, string FileName) where T : new()
        //{
        //    try
        //    {
        //        using (TextReader Reader = new StreamReader(FileName))
        //        {
        //            if (obj == null)
        //            {
        //                obj = new T();
        //            }
        //            XmlSerializer Serializer = new XmlSerializer(obj.GetType());

        //            T Result = (T)Serializer.Deserialize(Reader);

        //            return Result;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        return default(T);
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static T ToObject<T>(this byte[] Value)
        {
            MemoryStream Stream = new MemoryStream();
            BinaryFormatter Formatter = new BinaryFormatter();

            Stream.Write(Value, 0, Value.Length);
            Stream.Seek(0, SeekOrigin.Begin);

            T Result = (T)Formatter.Deserialize(Stream);

            return Result;

        }

        #endregion

        #region Object 

        /// <summary>
        /// Remove the specified Event from the target Object
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="EventName">The Event name to remove</param>
        public static void ClearEventInvocations(this object Value, string EventName)
        {
            if (Value != null)
            {
                FieldInfo fi = Value.GetType().GetEventField(EventName);

                if (fi == null)
                {
                    return;
                }

                fi.SetValue(Value, null);
            }
        }

        /// <summary>
        /// Convert the target object into an array of <see cref="Byte"/>
        /// </summary>
        /// <param name="Value"></param>
        /// <returns>Array of <see cref="Byte"/></returns>
        public static byte[] ToSerialisedByteArray(this object Value)
        {
            if (Value == null)
            {
                return null;
            }

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();

            bf.Serialize(ms, Value);

            return ms.ToArray();

        }

        #endregion

        #region type 

        /// <summary>
        /// Returns the specified Event from the target Object
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="EventName">The name of the Event to return</param>
        /// <returns></returns>
        public static FieldInfo GetEventField(this Type Value, string EventName)
        {
            FieldInfo Field = null;

            while (Value != null)
            {
                // Find events defined as field
                Field = Value.GetField(EventName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

                if (Field != null && (Field.FieldType == typeof(MulticastDelegate) || Field.FieldType.IsSubclassOf(typeof(MulticastDelegate))))
                {
                    break;
                }

                // Find events defined as property { add; remove; } 
                Field = Value.GetField("EVENT_" + EventName.ToUpper(), BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

                if (Field != null)
                {
                    break;
                }

                Value = Value.BaseType;
            }

            return Field;
        }

        #endregion

        #region string 

        /// <summary>
        /// Convert the given <see cref="string"/> into the target type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Value"></param>
        /// <returns>The target value, as the target type</returns>
        public static T As<T>(this string Value)
        {
            return As(Value, default(T));
        }

        /// <summary>
        /// Convert the given <see cref="string"/> into the target type.  If the value is null or Empty, return the default value instead
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Value"></param>
        /// <param name="DefaultValue">The value to return if the target value is null or Empty</param>
        /// <returns>The target value, as the target type</returns>
        public static T As<T>(this string Value, T DefaultValue)
        {
            if (typeof(T) == typeof(bool))
            {
                return (T)Convert.ChangeType(AsBool(Value,
                                                    Convert.ToBoolean(DefaultValue)),
                                                    typeof(T));
            }

            T result = default(T);

            if (String.IsNullOrEmpty(Value))
            {
                return DefaultValue;
            }

            try
            {
                Type underlyingType = Nullable.GetUnderlyingType(typeof(T));

                if (underlyingType == null)
                {
                    result = (T)Convert.ChangeType(Value, typeof(T));
                }
                else if (underlyingType == typeof(bool))
                {
                    result = (T)Convert.ChangeType(AsBool(Value,
                                                    Convert.ToBoolean(DefaultValue)),
                                                    typeof(T));
                }
                else
                {
                    result = (T)Convert.ChangeType(Value, underlyingType);
                }
            }
            finally
            {
            }

            return result;
        }

        /// <summary>
        /// Convert the given <see cref="string"/> into a <see cref="bool"/>
        /// </summary>
        /// <param name="Value"></param>
        /// <returns>The target value, as a <see cref="bool"/></returns>
        public static bool AsBool(this string Value)
        {
            return AsBool(Value, false);
        }

        /// <summary>
        /// Convert the given <see cref="string"/> into a <see cref="bool"/>.  If the value is null or Empty, return the default value instead
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="DefaultValue">The value to return if the target value is null or Empty</param>
        /// <returns>The target value, as a <see cref="bool"/></returns>
        public static bool AsBool(this string Value, bool DefaultValue)
        {
            if (String.IsNullOrEmpty(Value))
            {
                return DefaultValue;
            }

            switch (Value.ToLower())
            {
                case "1":
                case "t":
                case "true":
                    return true;
                case "0":
                case "f":
                case "false":
                    return false;
                default:
                    return DefaultValue;
            }
        }

        /// <summary>
        /// Search within the target <see cref="string"/> for the specified value
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="SearchString">The <see cref="string"/> to search for</param>
        /// <returns><see cref="bool"/> indicating success (true) or failure (false)</returns>
        public static bool Exists(this string Value, string SearchString)
        {
            bool Result = false;
            string[] StringToArray = Value.Split(',');

            if (Array.IndexOf(StringToArray, SearchString) > -1)
            {
                Result = true;
            }

            return Result;
        }

        /// <summary> 
        /// Return the length of the target <see cref="string"/> after performing a null coalescing check and Trim() 
        /// </summary> 
        /// <param name="Value">The <see cref="string"/> to query</param> 
        /// <returns>An <see cref="Int32"/> representing the length</returns> 
        public static int LengthOf(this string Value)
        {
            return (Value ?? string.Empty).Trim().Length;
        }

        public static string RemoveAlphaCharacters(this string Value)
        {
            return new string(Value.Where(c => char.IsDigit(c) || c == '.').ToArray());
        }

        public static string RemoveNumericCharacters(this string Value)
        {
            return new string(Value.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c) || c == '-').ToArray());
        }

        public static string Capitalise(this string Value)
        {
            string Result = Value;

            if (Value == null)
            {
                return null;
            }

            if (Value.Length > 1)
            {   // Capitalise everything to the right of a space or hyphen

                StringBuilder r = new StringBuilder();

                // First character is always uppercase
                r.Append(Value[0].ToString().ToUpper());

                for (int i = 1; i < Value.Length; i++)
                {
                    if (Value[i - 1] == (char)32 || Value[i - 1] == Convert.ToChar("-"))
                    {
                        r.Append(Value[i].ToString().ToUpper());
                    }
                    else
                    {
                        r.Append(Value[i].ToString());
                    }
                }

                Result = r.ToString();
            }
            else
            {
                return Value.ToUpper();
            }

            return Result;
        }

        /// <summary>
        /// Convert a string to an array of <see cref="byte"/>
        /// </summary>
        /// <param name="Value"></param>
        /// <returns>byte[] version of the input <see cref="string"/></returns>
        public static byte[] ToByteArray(this string Value)
        {
            byte[] Result = null;

            Result = Encoding.ASCII.GetBytes(Value);

            return Result;
        }

        #endregion

        #region byte[] 

        /// <summary>
        /// Convert an array of <see cref="byte"/> to a string
        /// </summary>
        /// <param name="Value"></param>
        /// <returns>String version of the input array of <see cref="byte"/></returns>
        public static string ToString(this byte[] Value)
        {
            string Result = null;

            Result = Encoding.ASCII.GetString(Value);

            return Result;
        }

        /// <summary>
        /// Convert an array of <see cref="byte"/> to a string
        /// </summary>
        /// <param name="Value"></param>
        /// <returns>String version of the input array of <see cref="byte"/></returns>
        public static string ToText(this byte[] Value, int Start, int Length)
        {
            byte[] Bytes = new byte[Length];
            string Result = "";
            Array.Copy(Value, Start, Bytes, 0, Length);

            for (int i = 0; i < Length; i++)
            {
                if (Bytes[i] != 0)
                {
                    Result += ((char)Bytes[i]).ToString();
                }
            }

            return (Result).Trim();
        }

        #endregion

        #region FileInfo 

        /// <summary>
        /// Print the target file on the default printer
        /// </summary>
        /// <param name="value"></param>
        public static void Print(this FileInfo value)
        {
            Process p = new Process();
            p.StartInfo.FileName = value.FullName;
            p.StartInfo.Verb = "Print";
            p.Start();
        }

        //public static void CopyTo(this FileInfo SourceInfo, FileInfo DestinationInfo, Action<int> ProgressCallbackMethod) 
        //{ 
        //    const int BUFFERSIZE = 1024 * 1024;    // 1MB 
        //    byte[] Buffer = new byte[BUFFERSIZE]; 
        //    byte[] Buffer2 = new byte[BUFFERSIZE]; 
        //    bool Swap = false; 
        //    int Progress = 0; 
        //    int ReportedProgress = 0; 
        //    int read = 0; 
        //    long len = SourceInfo.Length; 
        //    float flen = len; 
        //    Task writer = null; 

        //    using (FileStream SourceStream = SourceInfo.OpenRead()) 
        //    using (FileStream DestinationStream = DestinationInfo.OpenWrite()) 
        //    { 
        //        DestinationStream.SetLength(SourceStream.Length); 

        //        for (long size = 0; size < len; size += read) 
        //        { 
        //            if ((Progress = ((int)((size / flen) * 100))) != ReportedProgress) 
        //            { 
        //                ProgressCallbackMethod(ReportedProgress = Progress); 
        //            } 

        //            read = SourceStream.Read(Swap ? Buffer : Buffer2, 0, BUFFERSIZE); 

        //            writer?.Wait(); 

        //            writer = DestinationStream.WriteAsync(Swap ? Buffer : Buffer2, 0, read); 

        //            Swap = !Swap; 
        //        } 

        //        writer?.Wait(); 
        //    } 
        //} 

        //public static Task<Utilities.CopyResult> CopyTo(this FileInfo SourceInfo, FileInfo DestinationInfo, Action<int> ProgressCallback) 
        //{ 
        //    const int BUFFERSIZE = 1024 * 1024;    // 1MB 
        //    byte[] Buffer = new byte[BUFFERSIZE]; 
        //    byte[] Buffer2 = new byte[BUFFERSIZE]; 
        //    bool Swap = false; 
        //    int Progress = 0; 
        //    int ReportedProgress = 0; 
        //    long BytesRead = 0; 
        //    long SourceLength = SourceInfo.Length; 
        //    float SourceLength2 = SourceLength; 
        //    Task WriteTask = null; 
        //    TaskCompletionSource<Utilities.CopyResult> tcs = new TaskCompletionSource<Utilities.CopyResult>(); 

        //    try 
        //    { 
        //        using (FileStream SourceStream = SourceInfo.OpenRead()) 
        //        using (FileStream DestinationStream = DestinationInfo.OpenWrite()) 
        //        { 
        //            DestinationStream.SetLength(SourceStream.Length); 

        //            for (long ReadPosition = 0; ReadPosition < SourceLength; ReadPosition += BytesRead) 
        //            { 
        //                if ((Progress = ((int)((ReadPosition / SourceLength2) * 100))) != ReportedProgress) 
        //                { 
        //                    ProgressCallback(ReportedProgress = Progress); 
        //                } 

        //                BytesRead = SourceStream.Read(Swap ? Buffer : Buffer2, 0, BUFFERSIZE); 

        //                WriteTask?.Wait(); 

        //                WriteTask = DestinationStream.WriteAsync(Swap ? Buffer : Buffer2, 0, (int)BytesRead); 

        //                Swap = !Swap; 
        //            } 

        //            WriteTask?.Wait(); 

        //        } 

        //        ProgressCallback(100); 

        //        DestinationInfo.CreationTime = SourceInfo.CreationTime; 
        //        DestinationInfo.LastAccessTime = SourceInfo.LastAccessTime; 
        //        DestinationInfo.LastWriteTime = SourceInfo.LastWriteTime; 

        //        tcs.TrySetResult(Utilities.CopyResult.Success); 
        //    } 
        //    catch //(Exception e) 
        //    { 
        //        tcs.TrySetResult(Utilities.CopyResult.UnexpectedException); 
        //    } 

        //    return tcs.Task; 
        //} 

        /// <summary> 
        /// Permanently delete the file, attempting to do so MaximumAttempts time, with a pause of PauseTime (Milliseconds) between each attempt 
        /// </summary> 
        /// <param name="Instance">The FileInfo instance to delete</param> 
        /// <param name="MaximumAttempts">The maximum number of deletion attempts</param> 
        /// <param name="PauseTime">The time in Milliseconds to pause between deletion attempts</param> 
        /// <returns>True = Success</returns> 
        public static bool DeleteWithRetry(this System.IO.FileInfo Instance, int MaximumAttempts = 3, int PauseTime = 250)
        {
            bool Result = false;
            int DeleteCounter = 1;
            string Path = Instance.FullName;

            do
            {
                try
                {
                    System.IO.File.Delete(Path);
                    Result = true;
                }
                catch
                {
                    DeleteCounter++;
                    System.Threading.Thread.Sleep(PauseTime);
                }
            }
            while (Result == false && DeleteCounter < MaximumAttempts);

            return Result;
        }

        #endregion

        #region Bitmap 

        //public static Bitmap Rotate(this Bitmap bitmap, float angle) 
        //{ 
        //    using (Graphics graphics = Graphics.FromImage(bitmap)) 
        //    { 
        //        graphics.TranslateTransform((float)bitmap.Width / 2, (float)bitmap.Height / 2); 
        //        graphics.RotateTransform(angle); 
        //        graphics.TranslateTransform(-(float)bitmap.Width / 2, -(float)bitmap.Height / 2); 

        //        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic; 
        //        graphics.DrawImage(bitmap, new Point(0, 0)); 
        //    } 

        //    return bitmap; 
        //} 

        #endregion

        #region int 

        public static string ToMemorySize(this int value, int decimalPlaces = 0)
        {
            return ((long)value).ToMemorySize(decimalPlaces);
        }

        public static string FinancialQuarter(this int Value)
        {
            switch (Value)
            {
                case 1:
                    return "July";
                case 2:
                    return "October";
                case 3:
                    return "January";
                case 4:
                    return "April";
                default:
                    return "Error:  Invalid quarter for " + Value.ToString();
            }
        }

        public static string CalendarQuarter(this int Value)
        {
            switch (Value)
            {
                case 1:
                    return "January";
                case 2:
                    return "April";
                case 3:
                    return "July";
                case 4:
                    return "October";
                default:
                    return "Error:  Invalid quarter for " + Value.ToString();
            }
        }

        #endregion

        #region long 

        public static string ToMemorySize(this long value, int decimalPlaces = 0)
        {
            double asTb = Math.Round((double)value / OneTb, decimalPlaces);
            double asGb = Math.Round((double)value / OneGb, decimalPlaces);
            double asMb = Math.Round((double)value / OneMb, decimalPlaces);
            double asKb = Math.Round((double)value / OneKb, decimalPlaces);

            string chosenValue = asTb > 1 ? string.Format("{0} Tb", asTb)
                : asGb > 1 ? string.Format("{0} Gb", asGb)
                : asMb > 1 ? string.Format("{0} Mb", asMb)
                : asKb > 1 ? string.Format("{0} Kb", asKb)
                : string.Format("{0}B", Math.Round((double)value, decimalPlaces));
            return chosenValue;
        }

        #endregion

        #region XmlElement 

        public static string ValueToString(this XmlElement Node)
        {
            string Result = "";

            try
            {
                Result = Node.InnerText;
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static string[] ValueToStringArray(this XmlElement Node)
        {
            string[] Result = { };

            try
            {
                Result = (from string s in Node.InnerText
                          select s).ToArray();
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static ushort ValueToUInt16(this XmlElement Node)
        {
            ushort Result = 0;

            Result = Convert.ToUInt16(Node.InnerText);

            return Result;
        }

        public static short ValueToInt16(this XmlElement Node)
        {
            short Result = 0;

            try
            {
                Result = Convert.ToInt16(Node.InnerText);
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static short[] ValueToInt16Array(this XmlElement Node)
        {
            short[] Result = { };

            try
            {
                Result = (from string s in Node.InnerText
                          select Convert.ToInt16(s)).ToArray();
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static ushort[] ValueToUInt16Array(this XmlElement Node)
        {
            ushort[] Result = { };

            try
            {
                Result = (from string s in Node.InnerText
                          select Convert.ToUInt16(s)).ToArray();
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static uint ValueToUInt32(this XmlElement Node)
        {
            uint Result = 0;

            try
            {
                Result = Convert.ToUInt32(Node.InnerText);
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static int ValueToInt32(this XmlElement Node)
        {
            int Result = 0;

            try
            {
                Result = Convert.ToInt32(Node.InnerText);
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static int[] ValueToInt32Array(this XmlElement Node)
        {
            int[] Result = { };

            try
            {
                Result = (from string s in Node.InnerText
                          select Convert.ToInt32(s)).ToArray();
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static long ValueToInt64(this XmlElement Node)
        {
            long Result = 0;

            try
            {
                Result = Convert.ToInt64(Node.InnerText);
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static long[] ValueToInt64Array(this XmlElement Node)
        {
            long[] Result = { };

            try
            {
                Result = (from string s in Node.InnerText
                          select Convert.ToInt64(s)).ToArray();
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static ulong ValueToUInt64(this XmlElement Node)
        {
            ulong Result = 0;

            try
            {
                Result = Convert.ToUInt64(Node.InnerText);
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static ulong[] ValueToUInt64Array(this XmlElement Node)
        {
            ulong[] Result = { };

            try
            {
                Result = (from string s in Node.InnerText
                          select Convert.ToUInt64(s)).ToArray();
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static uint ValueToUInt(this XmlElement Node)
        {
            uint Result = 0;

            try
            {
                Result = Convert.ToUInt32(Node.InnerText);
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static int ValueToInt(this XmlElement Node)
        {
            int Result = 0;

            try
            {
                Result = Convert.ToInt32(Node.InnerText);
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static int[] ValueToIntArray(this XmlElement Node)
        {
            int[] Result = { };

            try
            {
                Result = (from string s in Node.InnerText
                          select (Convert.ToInt32(s))).ToArray();
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static uint[] ValueToUInt32Array(this XmlElement Node)
        {
            uint[] Result = { };

            try
            {
                Result = (from string s in Node.InnerText
                          select Convert.ToUInt32(s)).ToArray();
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static uint[] ValueToUIntArray(this XmlElement Node)
        {
            uint[] Result = { };

            try
            {
                Result = (from string s in Node.InnerText
                          select (uint)Convert.ToInt32(s)).ToArray();
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static DateTime ValueToDateTime(this XmlElement Node)
        {
            DateTime Result = DateTime.MinValue;

            try
            {
                Result = Convert.ToDateTime(Node.InnerText);
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static DateTime[] ValueToDateTimeArray(this XmlElement Node)
        {
            DateTime[] Result = { };

            try
            {
                Result = (from string s in Node.InnerText
                          select Convert.ToDateTime(s)).ToArray();
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static bool ValueToBool(this XmlElement Node)
        {
            bool Result = false;

            try
            {
                Result = Convert.ToBoolean(Node.InnerText);
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        public static bool[] ValueToBoolArray(this XmlElement Node)
        {
            bool[] Result = { };

            try
            {
                Result = (from string s in Node.InnerText
                          select Convert.ToBoolean(s)).ToArray();
            }
            catch
            {
                return Result;
            }

            return Result;
        }

        #endregion

        #region List<T> 

        /// <summary> 
        /// A generic function that loops a List of any class type, looking for a boolean field value. 
        /// If it finds a TRUE value, then exit the loop and return TRUE. 
        /// </summary> 
        /// <typeparam name="T">Generic reference</typeparam> 
        /// <param name="List">Refers to this (current List object)</param> 
        /// <param name="FieldName">The field containing the boolean value</param> 
        /// <returns>Boolean True or False</returns> 
        public static bool AreAnyOfTheseRequired<T>(this List<T> List, string FieldName)
        {
            bool returnValue = false;
            foreach (T item in List)
            {
                if ((bool)item.GetType().GetProperty(FieldName).GetValue(item, null))
                {
                    returnValue = true;
                    break;
                }
            }

            return returnValue;
        }

        public static bool Approximates<T>(this List<T> Value, List<T> Comparison)
        {
            return Value.Count == Comparison.Count &&
                   Value.All(Comparison.Contains) &&
                   Comparison.All(Value.Contains);
        }

        #endregion

        #region List<string> 

        /// <summary> 
        /// Converts a <see cref="List{string}"/> containing string values to a comma separated value. 
        /// </summary> 
        /// <param name="Values">Refers to this (current List object)</param> 
        /// <returns>A string containing the CSV</returns> 
        public static string ToCSV(this List<string> Values)
        {
            string returnValue = String.Empty;
            foreach (string value in Values)
            {
                returnValue += String.Format("{0},", value);
            }

            return returnValue.Remove(returnValue.Length - 1);
        }

        #endregion

        #region TimeSpan 

        public static string ToReadableAgeString(this TimeSpan span)
        {
            return string.Format("{0:0}", span.Days / 365.25);
        }

        public static string ToReadableString(this TimeSpan span)
        {
            string formatted = string.Format("{0}{1}{2}{3}",
                span.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", span.Days, span.Days == 1 ? String.Empty : "s") : string.Empty,
                span.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", span.Hours, span.Hours == 1 ? String.Empty : "s") : string.Empty,
                span.Duration().Minutes > 0 ? string.Format("{0:0} min{1}, ", span.Minutes, span.Minutes == 1 ? String.Empty : "s") : string.Empty,
                span.Duration().Seconds > 0 ? string.Format("{0:0} sec{1}", span.Seconds, span.Seconds == 1 ? String.Empty : "s") : string.Empty);

            if (formatted.EndsWith(", ", StringComparison.CurrentCulture))
            {
                formatted = formatted.Substring(0, formatted.Length - 2);
            }

            if (string.IsNullOrEmpty(formatted))
            {
                formatted = "0 seconds";
            }

            return formatted;
        }

        #endregion

        #region Assembly 

        ///// <summary> 
        ///// From https://stackoverflow.com/questions/1600962/displaying-the-build-date 
        ///// </summary> 
        ///// <param name="Assembly">An assembly to query</param> 
        ///// <param name="TimeZone">A TimeZoneInfo parameter</param> 
        ///// <returns>The BuildDate of the Assembly as a DateTime</returns> 
        //public static DateTime GetBuildDateTime(this Assembly Assembly, TimeZoneInfo TimeZone)
        //{
        //    // Constants related to the Windows PE file format. 
        //    const int PE_HEADER_OFFSET = 60;
        //    const int LINKER_TIMESTAMP_OFFSET = 8;

        //    // Discover the base memory address where our assembly is loaded 
        //    Module entryModule = Assembly.ManifestModule;
        //    IntPtr hMod = Marshal.GetHINSTANCE(entryModule);

        //    if (hMod == IntPtr.Zero - 1)
        //    {
        //        throw new Exception("Failed to get HINSTANCE.");
        //    }

        //    // Read the linker timestamp 
        //    int offset = Marshal.ReadInt32(hMod, PE_HEADER_OFFSET);
        //    int secondsSince1970 = Marshal.ReadInt32(hMod, offset + LINKER_TIMESTAMP_OFFSET);

        //    // Convert the timestamp to a DateTime 
        //    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        //    DateTime linkTimeUtc = epoch.AddSeconds(secondsSince1970);
        //    DateTime Result = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, TimeZone ?? TimeZoneInfo.Local);

        //    return Result;
        //}

        #endregion

        #region Enums 

        /// <summary> 
        /// Gets all items for an enum value. 
        /// </summary> 
        /// <typeparam name="T"></typeparam> 
        /// <param name="Value">The value.</param> 
        /// <returns></returns> 
        public static IEnumerable<T> GetAllItems<T>(this Enum Value)
        {
            foreach (object item in Enum.GetValues(typeof(T)))
            {
                yield return (T)item;
            }
        }

        /// <summary> 
        /// Gets all items for an enum type. 
        /// </summary> 
        /// <typeparam name="T"></typeparam>  
        /// <returns></returns> 
        public static IEnumerable<T> GetAllItems<T>() where T : struct
        {
            foreach (object item in Enum.GetValues(typeof(T)))
            {
                yield return (T)item;
            }
        }

        /// <summary> 
        /// Gets all combined items from an enum value. 
        /// </summary> 
        /// <typeparam name="T"></typeparam> 
        /// <param name="value">The value.</param> 
        /// <returns></returns> 
        /// <example> 
        /// Displays ValueA and ValueB. 
        /// <code> 
        /// EnumExample dummy = EnumExample.Combi; 
        /// foreach (var item in dummy.GetAllSelectedItems()) 
        /// { 
        ///    Console.WriteLine(item); 
        /// } 
        /// </code> 
        /// </example> 
        public static IEnumerable<T> GetAllSelectedItems<T>(this Enum value)
        {
            int valueAsInt = Convert.ToInt32(value, CultureInfo.InvariantCulture);

            foreach (object item in Enum.GetValues(typeof(T)))
            {
                int itemAsInt = Convert.ToInt32(item, CultureInfo.InvariantCulture);

                if (itemAsInt == (valueAsInt & itemAsInt))
                {
                    yield return (T)item;
                }
            }
        }

        /// <summary> 
        /// Determines whether the enum value contains a specific value. 
        /// </summary> 
        /// <param name="value">The value.</param> 
        /// <param name="request">The request.</param> 
        /// <returns> 
        ///     <c>true</c> if value contains the specified value; otherwise, <c>false</c>. 
        /// </returns> 
        /// <example> 
        /// <code> 
        /// EnumExample dummy = EnumExample.Combi; 
        /// if (dummy.Contains (EnumExample.ValueA)) 
        /// { 
        ///     Console.WriteLine("dummy contains EnumExample.ValueA"); 
        /// } 
        /// </code> 
        /// </example> 
        public static bool Contains<T>(this Enum value, T request)
        {
            int valueAsInt = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            int requestAsInt = Convert.ToInt32(request, CultureInfo.InvariantCulture);

            if (requestAsInt == (valueAsInt & requestAsInt))
            {
                return true;
            }

            return false;
        }


        #endregion

        #region DateTime

        public static int Age(this DateTime DateOfBirth)
        {
            int age = DateTime.Now.Year - DateOfBirth.Year;

            if (DateTime.Now < DateOfBirth.AddYears(age))
            {
                age--;
            }

            if (age < 0)
            {
                age = 0;
            }

            return age;
        }

        public static string FinancialQuarter(this DateTime Value)
        {
            if (Value.Month >= 7 && Value.Month <= 9)
            {
                return "July to September " + Value.Year;
            }
            else if (Value.Month >= 10 && Value.Month <= 12)
            {
                return "October to December " + Value.Year;
            }
            else if (Value.Month >= 1 && Value.Month <= 3)
            {
                return "January to March " + ((Value.Year)).ToString();
            }
            else
            {
                return "April to June " + ((Value.Year)).ToString();
            }
        }

        public static string CalendarQuarter(this DateTime Value)
        {
            if (Value.Month >= 1 && Value.Month <= 3)
            {
                return "January to March " + Value.Year;
            }
            else if (Value.Month >= 4 && Value.Month <= 6)
            {
                return "April to June " + Value.Year;
            }
            else if (Value.Month >= 7 && Value.Month <= 9)
            {
                return "July to September " + Value.Year;
            }
            else
            {
                return "October to December " + Value.Year;
            }
        }

        #endregion

        #region bool

        /// <summary>
        /// Convert to a Database bit
        /// </summary>
        /// <param name="Value"></param>
        /// <returns><see cref="Int16"/></returns>
        public static Int16 ToBit(this bool Value)
        {
            if (Value)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        #endregion

        #region DataRow

        public static string GetStringField(this System.Data.DataRow Row, string Field)
        {
            string Result = "";

            if (Row.Table.Columns.Contains(Field))
            {
                Result = Row[Field].ToString();
            }

            return Result;
        }

        public static int GetIntField(this System.Data.DataRow Row, string Field)
        {
            int Result = -1;

            if (Row.Table.Columns.Contains(Field))
            {
                Result = Convert.ToInt32(Row[Field].ToString());
            }

            return Result;
        }

        public static DateTime GetDateField(this System.Data.DataRow Row, string Field)
        {
            DateTime Result = DateTime.MinValue;

            if (Row.Table.Columns.Contains(Field))
            {
                Result = (DateTime)Row[Field];
            }

            return Result;
        }

        public static bool GetBoolField(this System.Data.DataRow Row, string Field)
        {
            bool Result = false;

            if (Row.Table.Columns.Contains(Field))
            {
                Result = (bool)Row[Field];
            }

            return Result;
        }

        #endregion

        #endregion

    }
}
