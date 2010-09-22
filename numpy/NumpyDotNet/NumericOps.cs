﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;
using IronPython.Runtime;
using IronPython.Modules;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using Microsoft.CSharp.RuntimeBinder;
using System.Dynamic;

namespace NumpyDotNet {


    /// <summary>
    /// Records the type-specific get/set items for each descriptor type.
    /// </summary>
    public class ArrFuncs {
        internal Func<long, ndarray, Object> GetItem { get; set; }
        internal Action<Object, long, ndarray> SetItem { get; set; }
    }



    /// <summary>
    /// Collection of getitem/setitem functions and operations on object types.
    /// These are mostly used as callbacks from the core and operate on native
    /// memory.
    /// </summary>
    internal static class NumericOps {
        private static ArrFuncs[] ArrFuncs = null;
        private static Object ArrFuncsSyncRoot = new Object();

        /// <summary>
        /// Returns the array of functions appropriate to a given type.  The actual
        /// functions in the array will vary with the type sizes in the native code.
        /// </summary>
        /// <param name="t">Native array type</param>
        /// <returns>Functions matching that type</returns>
        internal static ArrFuncs FuncsForType(NpyDefs.NPY_TYPES t) {
            if (ArrFuncs == null) {
                InitArrFuncs();
            }
            return ArrFuncs[(int)t];
        }

        private static void GetGetSetItems(int numBytes, 
            out Func<long, ndarray, Object> getter,
            out Action<Object, long, ndarray> setter,
            out Func<long, ndarray, Object> ugetter,
            out Action<Object, long, ndarray> usetter) {
            switch (numBytes) {
                case 4:
                    getter = NumericOps.getitemInt32;
                    setter = NumericOps.setitemInt32;
                    ugetter = NumericOps.getitemUInt32;
                    usetter = NumericOps.setitemUInt32;
                    break;

                case 8:
                    getter = NumericOps.getitemInt64;
                    setter = NumericOps.setitemInt64;
                    ugetter = NumericOps.getitemUInt64;
                    usetter = NumericOps.setitemUInt64;
                    break;
                    
                default:
                    throw new NotImplementedException(
                        String.Format("Numeric size of {0} is not yet implemented.", numBytes));
            }
        }


        /// <summary>
        /// Initializes the type-specific functions for each native type.
        /// </summary>
        private static void InitArrFuncs() {
            lock (ArrFuncsSyncRoot) {
                if (ArrFuncs == null) {
                    ArrFuncs[] arr = new ArrFuncs[(int)NpyDefs.NPY_TYPES.NPY_NTYPES];

                    Func<long, ndarray, Object> intGet, longGet, longLongGet;
                    Func<long, ndarray, Object> uintGet, ulongGet, ulongLongGet;
                    Action<Object, long, ndarray> intSet, longSet, longLongSet;
                    Action<Object, long, ndarray> uintSet, ulongSet, ulongLongSet;

                    GetGetSetItems(NpyCoreApi.Native_SizeOfInt, out intGet, out intSet,
                        out uintGet, out uintSet);
                    GetGetSetItems(NpyCoreApi.Native_SizeOfLong, out longGet, out longSet,
                        out ulongGet, out ulongSet);
                    GetGetSetItems(NpyCoreApi.Native_SizeOfLongLong, out longLongGet,
                        out longLongSet, out ulongLongGet, out ulongLongSet);

                    arr[(int)NpyDefs.NPY_TYPES.NPY_BOOL] =
                        new ArrFuncs() { GetItem = NumericOps.getitemBool, SetItem = NumericOps.setitemBool };
                    arr[(int)NpyDefs.NPY_TYPES.NPY_BYTE] =
                        new ArrFuncs() { GetItem = NumericOps.getitemByte, SetItem = NumericOps.setitemByte };
                    arr[(int)NpyDefs.NPY_TYPES.NPY_UBYTE] =
                        new ArrFuncs() { GetItem = NumericOps.getitemByte, SetItem = NumericOps.setitemByte };
                    arr[(int)NpyDefs.NPY_TYPES.NPY_SHORT] =
                        new ArrFuncs() { GetItem = NumericOps.getitemShort, SetItem = NumericOps.setitemShort };
                    arr[(int)NpyDefs.NPY_TYPES.NPY_USHORT] =
                        new ArrFuncs() { GetItem = NumericOps.getitemUShort, SetItem = NumericOps.setitemUShort };
                    arr[(int)NpyDefs.NPY_TYPES.NPY_INT] =
                        new ArrFuncs() { GetItem = intGet, SetItem = intSet };
                    arr[(int)NpyDefs.NPY_TYPES.NPY_UINT] =
                        new ArrFuncs() { GetItem = uintGet, SetItem = uintSet };
                    arr[(int)NpyDefs.NPY_TYPES.NPY_LONG] =
                        new ArrFuncs() { GetItem = longGet, SetItem = longSet };
                    arr[(int)NpyDefs.NPY_TYPES.NPY_ULONG] =
                        new ArrFuncs() { GetItem = ulongGet, SetItem = ulongSet };
                    arr[(int)NpyDefs.NPY_TYPES.NPY_LONGLONG] =
                        new ArrFuncs() { GetItem = longLongGet, SetItem = longLongSet };
                    arr[(int)NpyDefs.NPY_TYPES.NPY_ULONGLONG] =
                        new ArrFuncs() { GetItem = ulongLongGet, SetItem = ulongLongSet };
                    arr[(int)NpyDefs.NPY_TYPES.NPY_FLOAT] =
                        new ArrFuncs() { GetItem = NumericOps.getitemFloat, SetItem = NumericOps.setitemFloat };
                    arr[(int)NpyDefs.NPY_TYPES.NPY_DOUBLE] =
                        new ArrFuncs() { GetItem = NumericOps.getitemDouble, SetItem = NumericOps.setitemDouble };
                    arr[(int)NpyDefs.NPY_TYPES.NPY_LONGDOUBLE] = null;
                    arr[(int)NpyDefs.NPY_TYPES.NPY_CFLOAT] = null;
                    arr[(int)NpyDefs.NPY_TYPES.NPY_CDOUBLE] =
                        new ArrFuncs() { GetItem = NumericOps.getitemCDouble, SetItem = NumericOps.setitemCDouble };
                    arr[(int)NpyDefs.NPY_TYPES.NPY_CLONGDOUBLE] = null;
                    arr[(int)NpyDefs.NPY_TYPES.NPY_DATETIME] = null;
                    arr[(int)NpyDefs.NPY_TYPES.NPY_TIMEDELTA] = null;
                    arr[(int)NpyDefs.NPY_TYPES.NPY_OBJECT] =
                        new ArrFuncs() { GetItem = NumericOps.getitemObject, SetItem = NumericOps.setitemObject };
                    arr[(int)NpyDefs.NPY_TYPES.NPY_STRING] =
                        new ArrFuncs() { GetItem = NumericOps.getitemString, SetItem = NumericOps.setitemString };
                    arr[(int)NpyDefs.NPY_TYPES.NPY_UNICODE] = null;
                    arr[(int)NpyDefs.NPY_TYPES.NPY_VOID] =
                        new ArrFuncs() { GetItem = NumericOps.getitemVOID, SetItem = NumericOps.setitemVOID };

                    ArrFuncs = arr;
                }
            }
        }


        #region GetItem functions
        internal static Object getitemBool(long offset, ndarray arr) {
            bool f;

            unsafe {
                bool* p = (bool*)((byte *)arr.data.ToPointer() + offset);
                f = *p;
            }
            return f;
        }

        // Both Byte and UByte
        internal static Object getitemByte(long offset, ndarray arr) {
            byte f;

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                f = *p;
            }
            return f;
        }


        internal static Object getitemShort(long offset, ndarray arr) {
            short f;

            unsafe {
                byte* p = (byte *)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    f = *(short*)p;
                } else {
                    CopySwap2((byte*)&f, p, !arr.IsNotSwapped);
                }
            }
            return f;
        }

        internal static Object getitemUShort(long offset, ndarray arr) {
            ushort f;

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    f = *(ushort*)p;
                } else {
                    CopySwap2((byte*)&f, p, !arr.IsNotSwapped);
                }
            }
            return f;
        }

        internal static Object getitemInt32(long offset, ndarray arr) {
            int f;

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    f = *(int*)p;
                } else {
                    CopySwap4((byte*)&f, p, !arr.IsNotSwapped);
                }
            }
            return f;
        }

        internal static Object getitemUInt32(long offset, ndarray arr) {
            uint f;

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    f = *(uint*)p;
                } else {
                    CopySwap4((byte*)&f, p, !arr.IsNotSwapped);
                }
            }
            return f;
        }

        internal static Object getitemInt64(long offset, ndarray arr) {
            long f;

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    f = *(long*)p;
                } else {
                    CopySwap8((byte*)&f, p, !arr.IsNotSwapped);
                }
            }
            return f;
        }

        internal static Object getitemUInt64(long offset, ndarray arr) {
            ulong f;

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    f = *(ulong*)p;
                } else {
                    CopySwap8((byte*)&f, p, !arr.IsNotSwapped);
                }
            }
            return f;
        }

        internal static Object getitemFloat(long offset, ndarray arr) {
            float f;

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    f = *(float*)p;
                } else {
                    CopySwap4((byte*)&f, p, !arr.IsNotSwapped);
                }
            }
            return f;
        }

        internal static Object getitemDouble(long offset, ndarray arr) {
            double f;

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    f = *(double*)p;
                } else {
                    CopySwap8((byte*)&f, p, !arr.IsNotSwapped);
                }
            }
            return f;
        }

        internal static Object getitemCDouble(long offset, ndarray arr) {
            Complex f;

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    f = new Complex(*(double*)p, *((double*)p + 1));
                } else {
                    double r, i;
                    CopySwap8((byte*)&r, p, !arr.IsNotSwapped);
                    CopySwap8((byte*)&i, (byte*)((double*)p + 1), !arr.IsNotSwapped);
                    f = new Complex(r, i);
                }
            }
            return f;
        }

        internal static Object getitemObject(long offset, ndarray arr) {
            IntPtr f;

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    switch (IntPtr.Size) {
                        case 4:
                            f = new IntPtr(*(int*)p);
                            break;
                        case 8:
                            f = new IntPtr(*(long*)p);
                            break;
                        default:
                            throw new NotImplementedException(
                                String.Format("IntPtr of size {0} is not supported.", IntPtr.Size));
                    }
                } else if (IntPtr.Size == 4) {
                    int r;
                    CopySwap4((byte*)&r, p, !arr.IsNotSwapped);
                    f = new IntPtr(*(int*)p);
                } else if (IntPtr.Size == 8) {
                    long r;
                    CopySwap8((byte*)&r, p, !arr.IsNotSwapped);
                    f = new IntPtr(*(long*)p);
                } else {
                    throw new NotImplementedException(
                        String.Format("IntPtr of size {0} is not implemented.", IntPtr.Size));
                }
            }
            return GCHandle.FromIntPtr(f).Target;
        }

        internal static Object getitemString(long offset, ndarray arr) {
            IntPtr p = new IntPtr(arr.data.ToInt64() + offset);
            String s = Marshal.PtrToStringAnsi(p, arr.dtype.ElementSize);
            return s.TrimEnd((char)0);
        }

        internal static Object getitemVOID(long offset, ndarray arr) {
            dtype d = arr.dtype;
            if (d.HasNames) {
                List<string> names = d.Names;
                object[] result = new object[names.Count];
                Int32 savedflags = arr.RawFlags;
                try {
                    int i = 0;
                    foreach (string name in names) {
                        NpyCoreApi.NpyArray_DescrField field = NpyCoreApi.GetDescrField(d, name);
                        dtype field_dtype = NpyCoreApi.ToInterface<dtype>(field.descr);
                        Marshal.WriteIntPtr(arr.Array, NpyCoreApi.ArrayOffsets.off_descr, field.descr);
                        int alignment = arr.dtype.Alignment;
                        if (alignment > 1 &&
                            (offset + field.offset) % alignment != 0) {
                            arr.RawFlags = savedflags & ~NpyDefs.NPY_ALIGNED;
                        } else {
                            arr.RawFlags = savedflags | NpyDefs.NPY_ALIGNED;
                        }
                        result[i++] = field_dtype.f.GetItem(offset + field.offset, arr);
                    }
                    return new PythonTuple(result);
                } finally {
                    arr.RawFlags = savedflags;
                    Marshal.WriteIntPtr(arr.Array, NpyCoreApi.ArrayOffsets.off_descr, d.Descr);
                }
            } else {
                throw new NotImplementedException("VOID type only implemented for fields");
            }
        }

        #endregion


        #region SetItem methods

        internal static void setitemBool(Object o, long offset, ndarray arr) {
            bool f;

            if (o is Boolean) f = (bool)o;
            else if (o is IConvertible) f = ((IConvertible)o).ToBoolean(null);
            else throw new NotImplementedException("Elvis has just left Wichita.");

            unsafe {
                bool* p = (bool*)((byte *)arr.data.ToPointer() + offset);
                *p = f;
            }
        }

        internal static void setitemByte(Object o, long offset, ndarray arr) {
            byte f;

            if (o is Byte) f = (byte)o;
            else if (o is IConvertible) f = ((IConvertible)o).ToByte(null);
            else throw new NotImplementedException("Elvis has just left Wichita.");

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                *p = f;
            }
        }

        internal static void setitemShort(Object o, long offset, ndarray arr) {
            short f;

            if (o is Int16) f = (short)o;
            else if (o is IConvertible) f = ((IConvertible)o).ToInt16(null);
            else throw new NotImplementedException("Elvis has just left Wichita.");

            unsafe {
                byte* p = (byte *)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    *(short*)p = f;
                } else {
                    CopySwap2(p, (byte*)&f, !arr.IsNotSwapped);
                }
            }
        }

        internal static void setitemUShort(Object o, long offset, ndarray arr) {
            ushort f;

            if (o is UInt16) f = (ushort)o;
            else if (o is IConvertible) f = ((IConvertible)o).ToUInt16(null);
            else throw new NotImplementedException("Elvis has just left Wichita.");

            unsafe {
                byte* p = (byte *)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    *(ushort*)p = f;
                } else {
                    CopySwap2(p, (byte*)&f, !arr.IsNotSwapped);
                }
            }
        }

        internal static void setitemInt32(Object o, long offset, ndarray arr) {
            int f;

            if (o is Int32) f = (int)o;
            else if (o is IConvertible) f = ((IConvertible)o).ToInt32(null);
            else if (o is BigInteger) ((BigInteger)o).AsInt32(out f);
            else throw new NotImplementedException("Elvis has just left Wichita.");

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    *(int*)p = f;
                } else {
                    CopySwap4(p, (byte*)&f, !arr.IsNotSwapped);
                }
            }
        }

        internal static void setitemUInt32(Object o, long offset, ndarray arr) {
            uint f;

            if (o is UInt32) f = (uint)o;
            else if (o is IConvertible) f = ((IConvertible)o).ToUInt32(null);
            else if (o is BigInteger) ((BigInteger)o).AsUInt32(out f);
            else throw new NotImplementedException("Elvis has just left Wichita.");

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    *(uint*)p = f;
                } else {
                    CopySwap4(p, (byte*)&f, !arr.IsNotSwapped);
                }
            }
        }

        internal static void setitemInt64(Object o, long offset, ndarray arr) {
            long f;

            if (o is Int64) f = (long)o;
            else if (o is IConvertible) f = ((IConvertible)o).ToInt64(null);
            else if (o is BigInteger) ((BigInteger)o).AsInt64(out f);
            else throw new NotImplementedException("Elvis has just left Wichita.");

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    *(long*)p = f;
                } else {
                    CopySwap8(p, (byte*)&f, !arr.IsNotSwapped);
                }
            }
        }

        internal static void setitemUInt64(Object o, long offset, ndarray arr) {
            ulong f;

            if (o is UInt64) f = (ulong)o;
            else if (o is IConvertible) f = ((IConvertible)o).ToUInt64(null);
            else if (o is BigInteger) ((BigInteger)o).AsUInt64(out f);
            else throw new NotImplementedException("Elvis has just left Wichita.");

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    *(ulong*)p = f;
                } else {
                    CopySwap8(p, (byte*)&f, !arr.IsNotSwapped);
                }
            }
        }

        internal static void setitemFloat(Object o, long offset, ndarray arr) {
            float f;

            if (o is Single) f = (float)o;
            else if (o is IConvertible) f = ((IConvertible)o).ToSingle(null);
            else throw new NotImplementedException("Elvis has just left Wichita.");

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    *(float*)p = f;
                } else {
                    CopySwap4(p, (byte*)&f, !arr.IsNotSwapped);
                }
            }
        }

        internal static void setitemDouble(Object o, long offset, ndarray arr) {
            double f;

            if (o is Double) f = (double)o;
            else if (o is IConvertible) f = ((IConvertible)o).ToDouble(null);
            else throw new NotImplementedException(
                String.Format("Elvis has just left Wichita (type {0}).", o.GetType().Name));

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    *(double*)p = f;
                } else {
                    CopySwap8(p, (byte*)&f, !arr.IsNotSwapped);
                }
            }
        }


        internal static void setitemCDouble(Object o, long offset, ndarray arr) {
            Complex f;

            if (o is Complex) f = (Complex)o;
            else if (o is IConvertible) {
                double d = ((IConvertible)o).ToDouble(null);
                f = new Complex(d, 0.0);
            } else throw new NotImplementedException(
                String.Format("Elvis has just left Wichita (type {0}).", o.GetType().Name));

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    *(double*)p = f.Real;
                    *((double*)p + 1) = f.Imaginary;
                } else {
                    double r = f.Real;
                    double i = f.Imaginary;
                    CopySwap8(p, (byte*)&r, !arr.IsNotSwapped);
                    CopySwap8(p, (byte*)&i, !arr.IsNotSwapped);
                }
            }
        }



        internal static void setitemObject(Object o, long offset, ndarray arr) {
            IntPtr f = GCHandle.ToIntPtr(GCHandle.Alloc(o));
            IntPtr prev = IntPtr.Zero;

            unsafe {
                byte* p = (byte*)arr.data.ToPointer() + offset;
                if (arr.IsBehaved) {
                    switch (IntPtr.Size) {
                        case 4:
                            prev = new IntPtr(* (int*)p);
                            *(int*)p = (int)f;
                            break;
                        case 8:
                            prev = new IntPtr(* (int*)p);
                            *(long*)p = (long)f;
                            break;
                        default:
                            throw new NotImplementedException(
                                String.Format("IntPtr size of {0} is not supported.", IntPtr.Size));
                    }
                } else if (IntPtr.Size == 4) {
                    int r;
                    CopySwap4((byte *)&r, p, !arr.IsNotSwapped);
                    prev = new IntPtr(r);
                    r = (int)f;
                    CopySwap4(p, (byte*)&r, !arr.IsNotSwapped);
                } else if (IntPtr.Size == 4) {
                    long r;
                    CopySwap8((byte *)&r, p, !arr.IsNotSwapped);
                    prev = new IntPtr(r);
                    r = (long)f;
                    CopySwap8(p, (byte*)&r, !arr.IsNotSwapped);
                } else {
                    throw new NotImplementedException(
                        String.Format("IntPtr size of {0} is not supported.", IntPtr.Size));
                }                    
            }

            // Release our handle to any previous object.
            if (prev != IntPtr.Zero) {
                GCHandle.FromIntPtr(prev).Free();
            }
        }

        internal static void setitemString(Object o, long offset, ndarray arr) {
            string s = o.ToString();
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            int elsize = arr.dtype.ElementSize;
            int copySize = Math.Min(bytes.Length, elsize);
            int i;
            IntPtr p = new IntPtr(arr.data.ToInt64() + offset);
            for (i = 0; i < copySize; i++) {
                Marshal.WriteByte(p, i, bytes[i]);
            }
            for (; i < elsize; i++) {
                Marshal.WriteByte(p, i, (byte)0);
            }
        }

        internal static void setitemVOID(object value, long offset, ndarray arr) {
            dtype d = arr.dtype;
            PythonTuple t = (value as PythonTuple);
            if (d.HasNames && t != null) {
                List<string> names = d.Names;
                if (names.Count != t.Count) {
                    throw new ArgumentException("size of tuple must match number of fields");
                }
                Int32 savedflags = arr.RawFlags;
                try {
                    int i = 0;
                    foreach (string name in names) {
                        NpyCoreApi.NpyArray_DescrField field = NpyCoreApi.GetDescrField(d, name);
                        dtype field_dtype = NpyCoreApi.ToInterface<dtype>(field.descr);
                        Marshal.WriteIntPtr(arr.Array, NpyCoreApi.ArrayOffsets.off_descr, field.descr);
                        int alignment = arr.dtype.Alignment;
                        if (alignment > 1 &&
                            (offset + field.offset) % alignment != 0) {
                            arr.RawFlags = savedflags & ~NpyDefs.NPY_ALIGNED;
                        } else {
                            arr.RawFlags = savedflags | NpyDefs.NPY_ALIGNED;
                        }
                        field_dtype.f.SetItem(t[i++], offset + field.offset, arr);
                    }
                } finally {
                    arr.RawFlags = savedflags;
                    Marshal.WriteIntPtr(arr.Array, NpyCoreApi.ArrayOffsets.off_descr, d.Descr);
                }
            } else {
                throw new NotImplementedException("VOID type only implemented for fields");
            }
        }
        #endregion

        #region Copy ops for swapping and unaligned access
        /// <summary>
        /// Copies two bytes from src to dest, optionally swapping the order
        /// for a change of endianess.  Either way, unaligned access is handled correctly.
        /// </summary>
        /// <param name="dest">Destination pointer</param>
        /// <param name="src">Source pointer</param>
        /// <param name="swap">True swaps byte order, false preserves the byte ordering</param>
        private unsafe static void CopySwap2(byte* dest, byte* src, bool swap) {
            if (!swap) {
                dest[0] = src[0];
                dest[1] = src[1];
            } else {
                dest[0] = src[1];
                dest[1] = src[0];
            }
        }

        private unsafe static void CopySwap4(byte* dest, byte* src, bool swap) {
            if (!swap) {
                dest[0] = src[0];
                dest[1] = src[1];
                dest[2] = src[2];
                dest[3] = src[3];
            } else {
                dest[0] = src[3];
                dest[1] = src[2];
                dest[2] = src[1];
                dest[3] = src[0];
            }
        }

        private unsafe static void CopySwap8(byte* dest, byte* src, bool swap) {
            if (!swap) {
                dest[0] = src[0];
                dest[1] = src[1];
                dest[2] = src[2];
                dest[3] = src[3];
                dest[4] = src[4];
                dest[5] = src[5];
                dest[6] = src[6];
                dest[7] = src[7];
            } else {
                dest[0] = src[7];
                dest[1] = src[6];
                dest[2] = src[5];
                dest[3] = src[4];
                dest[4] = src[3];
                dest[5] = src[2];
                dest[6] = src[1];
                dest[7] = src[0];
            }
        }
        #endregion

        #region Comparison Functions

        private static Object SyncRoot = new Object();
        private static LanguageContext PyContext = null;
        private static CallSite<Func<CallSite, Object, Object, Object>> Site_Equal;
        private static CallSite<Func<CallSite, Object, Object, Object>> Site_NotEqual;
        private static CallSite<Func<CallSite, Object, Object, Object>> Site_Greater;
        private static CallSite<Func<CallSite, Object, Object, Object>> Site_GreaterEqual;
        private static CallSite<Func<CallSite, Object, Object, Object>> Site_Less;
        private static CallSite<Func<CallSite, Object, Object, Object>> Site_LessEqual;
        private static CallSite<Func<CallSite, Object, int>> Site_Sign;

        private static CallSite<Func<CallSite, Object, Object, Object>> Site_Add;
        private static CallSite<Func<CallSite, Object, Object, Object>> Site_Subtract;
        private static CallSite<Func<CallSite, Object, Object, Object>> Site_Multiply;
        private static CallSite<Func<CallSite, Object, Object, Object>> Site_Divide;
        private static CallSite<Func<CallSite, Object, Object>> Site_Negative;
        private static CallSite<Func<CallSite, Object, Object>> Site_Abs;

        private static CallSite<Func<CallSite, Object, Object, Object>> Site_Power;
        private static CallSite<Func<CallSite, Object, Object, Object>> Site_Remainder;
        private static CallSite<Func<CallSite, Object, Object>> Site_Not;
        private static CallSite<Func<CallSite, Object, Object, Object>> Site_And;
        private static CallSite<Func<CallSite, Object, Object, Object>> Site_Or;
        private static CallSite<Func<CallSite, Object, Object, Object>> Site_Xor;
        private static CallSite<Func<CallSite, Object, Object, Object>> Site_LShift;
        private static CallSite<Func<CallSite, Object, Object, Object>> Site_RShift;

        internal static void InitUFuncOps(LanguageContext cntx) {
            // Fast escape which will occur all except the first time.
            if (PyContext != null) {
                if (PyContext != cntx) {
                    // I don't think this can happen, but just in case...
                    throw new NotImplementedException("Internal error: multiply IronPython contexts are not supported.");
                }
                return;
            }

            lock (SyncRoot) {
                if (PyContext == null) {
                    
                    // Construct the call sites for each operation we will need. This is much
                    // faster than constructing/destroying them with each loop.
                    Site_Equal = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                        cntx.CreateBinaryOperationBinder(System.Linq.Expressions.ExpressionType.Equal));
                    Site_NotEqual = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                        cntx.CreateBinaryOperationBinder(System.Linq.Expressions.ExpressionType.NotEqual));
                    Site_Greater = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                        cntx.CreateBinaryOperationBinder(System.Linq.Expressions.ExpressionType.GreaterThan));
                    Site_GreaterEqual = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                        cntx.CreateBinaryOperationBinder(System.Linq.Expressions.ExpressionType.GreaterThanOrEqual));
                    Site_Less = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                        cntx.CreateBinaryOperationBinder(System.Linq.Expressions.ExpressionType.LessThan));
                    Site_LessEqual = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                        cntx.CreateBinaryOperationBinder(System.Linq.Expressions.ExpressionType.LessThanOrEqual));

                    Site_Add = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                        cntx.CreateBinaryOperationBinder(System.Linq.Expressions.ExpressionType.Add));
                    Site_Subtract = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                        cntx.CreateBinaryOperationBinder(System.Linq.Expressions.ExpressionType.Subtract));
                    Site_Multiply = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                        cntx.CreateBinaryOperationBinder(System.Linq.Expressions.ExpressionType.Multiply));
                    Site_Divide = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                        cntx.CreateBinaryOperationBinder(System.Linq.Expressions.ExpressionType.Divide));
                    Site_Negative = CallSite<Func<CallSite, Object, Object>>.Create(
                        cntx.CreateUnaryOperationBinder(System.Linq.Expressions.ExpressionType.Negate));

                    Site_Abs = CallSite<Func<CallSite, Object, Object>>.Create(
                        Binder.InvokeMember(CSharpBinderFlags.None, "__abs__",
                        null, typeof(NumericOps),
                        new CSharpArgumentInfo[] { 
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                        }));

                    Site_Power = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                        cntx.CreateBinaryOperationBinder(System.Linq.Expressions.ExpressionType.Power));
                    Site_Remainder = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                        cntx.CreateBinaryOperationBinder(System.Linq.Expressions.ExpressionType.Modulo));
                    Site_Not = CallSite<Func<CallSite, Object, Object>>.Create(
                        cntx.CreateUnaryOperationBinder(System.Linq.Expressions.ExpressionType.Not));
                    Site_And = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                        cntx.CreateBinaryOperationBinder(System.Linq.Expressions.ExpressionType.And));
                    Site_Or = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                        cntx.CreateBinaryOperationBinder(System.Linq.Expressions.ExpressionType.Or));
                    Site_Xor = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                        cntx.CreateBinaryOperationBinder(System.Linq.Expressions.ExpressionType.ExclusiveOr));
                    Site_LShift = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                        cntx.CreateBinaryOperationBinder(System.Linq.Expressions.ExpressionType.LeftShift));
                    Site_RShift = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                        cntx.CreateBinaryOperationBinder(System.Linq.Expressions.ExpressionType.RightShift));

                    
                    
                    // Set this last so any other accesses will block while we create
                    // the sites.
                    PyContext = cntx;
                }
            }
        }


        /// <summary>
        /// Cache of method call sites taking zero arguments.
        /// </summary>
        private static Dictionary<string, CallSite<Func<CallSite, Object, Object>>> ZeroArgMethodSites =
            new Dictionary<string, CallSite<Func<CallSite, Object, Object>>>();

        /// <summary>
        /// Cache of method call sites taking one argument.
        /// </summary>
        private static Dictionary<string, CallSite<Func<CallSite, Object, Object, Object>>> OneArgMethodSites =
            new Dictionary<string, CallSite<Func<CallSite, Object, Object, Object>>>();

        /// <summary>
        /// Executes a specified method taking one argument on an object. In order to
        /// be efficient, each method name is cached with the call site instance so
        /// future calls (this will likely be called in a loop) execute faster.
        /// 
        /// Passing IntPtr.Zero for argPtr causes it to execute a zero-argument method,
        /// otherwise it executes a one-argument method.  No facility is in place for
        /// passing null to a one-argument method.
        /// </summary>
        /// <param name="objPtr">Object method should be invoked on</param>
        /// <param name="methodName">Method name</param>
        /// <param name="argPtr">Optional argument, pass IntPtr.Zero if not needed</param>
        /// <returns>IntPtr to GCHandle of result object</returns>
        unsafe internal static IntPtr MethodCall(IntPtr objPtr, sbyte *methodName, IntPtr argPtr) {
            Object obj = GCHandle.FromIntPtr(objPtr).Target;
            Object result = null;
            String method = new String(methodName);

            if (argPtr != IntPtr.Zero) {
                Object arg = GCHandle.FromIntPtr(argPtr).Target;
                CallSite<Func<CallSite, Object, Object, Object>> site;

                // Cache the call site object based on method name.
                lock (OneArgMethodSites) {
                    if (!OneArgMethodSites.TryGetValue(method, out site)) {
                        site = CallSite<Func<CallSite, Object, Object, Object>>.Create(
                            Binder.InvokeMember(CSharpBinderFlags.None, method,
                            null, typeof(NumericOps),
                            new CSharpArgumentInfo[] { 
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                            }));
                        OneArgMethodSites.Add(method, site);
                    }
                }
                result = site.Target(site, obj, arg);
            } else {
                CallSite<Func<CallSite, Object, Object>> site;

                lock (ZeroArgMethodSites) {
                    if (!ZeroArgMethodSites.TryGetValue(method, out site)) {
                        site = CallSite<Func<CallSite, Object, Object>>.Create(
                            Binder.InvokeMember(CSharpBinderFlags.None, method,
                            null, typeof(NumericOps),
                            new CSharpArgumentInfo[] { 
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) 
                            }));
                        ZeroArgMethodSites.Add(method, site);
                    }
                }
                result = site.Target(site, obj);
            }
            return (result != null) ? GCHandle.ToIntPtr(GCHandle.Alloc(result)) : IntPtr.Zero;
        }
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe internal delegate IntPtr del_MethodCall(IntPtr a, sbyte *b, IntPtr arg);
        

        /// <summary>
        /// Generic comparison function.  First argument should be bound to one of
        /// the callsite operations.
        /// </summary>
        /// <param name="site">CallSite operation to be performed</param>
        /// <param name="aPtr">First argument</param>
        /// <param name="bPtr">Second argument</param>
        /// <returns>1 if true, 0 if false, -1 on error</returns>
        private static int GenericCmp(CallSite<Func<CallSite, Object, Object, Object>> site,
            IntPtr aPtr, IntPtr bPtr) {
            Object a = GCHandle.FromIntPtr(aPtr).Target;
            Object b = GCHandle.FromIntPtr(bPtr).Target;
            return (bool)site.Target(Site_Equal, a, b) ? 1 : 0;
        }
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int del_GenericCmp(IntPtr a, IntPtr b);


        /// <summary>
        /// Generic unary operation.  First argument should be bound to a binary
        /// callsite function.
        /// </summary>
        /// <param name="site">Callsite of some binary operation to perform</param>
        /// <param name="aPtr">Function argument</param>
        /// <returns>IntPtr to GCHandle referencing the result</returns>
        private static IntPtr GenericUnaryOp(CallSite<Func<CallSite, Object, Object>> site,
            IntPtr aPtr) {
            Object a = GCHandle.FromIntPtr(aPtr).Target;
            Object r = site.Target(site, a);
            return GCHandle.ToIntPtr(GCHandle.Alloc(r));
        }
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr del_GenericUnaryOp(IntPtr a);


        /// <summary>
        /// Generic binary operation.  First argument should be bound to a binary
        /// callsite function.
        /// </summary>
        /// <param name="site">Callsite of some binary operation to perform</param>
        /// <param name="aPtr">First argument</param>
        /// <param name="bPtr">Second argument</param>
        /// <returns>IntPtr to GCHandle referencing the result</returns>
        private static IntPtr GenericBinOp(CallSite<Func<CallSite, Object, Object, Object>> site,
            IntPtr aPtr, IntPtr bPtr) {
            Object a = GCHandle.FromIntPtr(aPtr).Target;
            Object b = GCHandle.FromIntPtr(bPtr).Target;
            Object r = site.Target(site, a, b);
            return GCHandle.ToIntPtr(GCHandle.Alloc(r));
        }
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr del_GenericBinOp(IntPtr a, IntPtr b);

        //static internal Func<IntPtr, IntPtr, int> Compare_Equal =
        //    (a, b) => GenericCmp(Site_Equal, a, b);
        static internal del_GenericCmp Compare_Equal =
            (a, b) => GenericCmp(Site_Equal, a, b);
        static internal del_GenericCmp  Compare_NotEqual =
            (a, b) => GenericCmp(Site_NotEqual, a, b);
        static internal del_GenericCmp Compare_Greater =
            (a, b) => GenericCmp(Site_Greater, a, b);
        static internal del_GenericCmp Compare_GreaterEqual =
            (a, b) => GenericCmp(Site_GreaterEqual, a, b);
        static internal del_GenericCmp Compare_Less =
            (a, b) => GenericCmp(Site_Less, a, b);
        static internal del_GenericCmp Compare_LessEqual =
            (a, b) => GenericCmp(Site_LessEqual, a, b);
        
        static internal del_GenericBinOp Op_Add =
            (a, b) => GenericBinOp(Site_Add, a, b);
        static internal del_GenericBinOp Op_Subtract =
            (a, b) => GenericBinOp(Site_Subtract, a, b);
        static internal del_GenericBinOp Op_Multiply =
            (a, b) => GenericBinOp(Site_Multiply, a, b);
        static internal del_GenericBinOp Op_Divide =
            (a, b) => GenericBinOp(Site_Divide, a, b);
        static internal del_GenericUnaryOp Op_Negate = 
            a => GenericUnaryOp(Site_Negative, a);
        static internal del_GenericUnaryOp Op_Sign = aPtr => {
            Object a = GCHandle.FromIntPtr(aPtr).Target;
            int result;

            if ((bool)Site_Less.Target(Site_Less, a, 0.0)) result = -1;
            else if ((bool)Site_Greater.Target(Site_Greater, a, 0.0)) result = 1;
            else result = 0;
            return GCHandle.ToIntPtr(GCHandle.Alloc(result));
        };
        static internal del_GenericUnaryOp Op_Absolute =
            a => GenericUnaryOp(Site_Abs, a);

        // TODO: trueDivide
        // TODO: floorDivide

        static internal del_GenericBinOp Op_Remainder =
            (a, b) => GenericBinOp(Site_Remainder, a, b);

        static internal del_GenericUnaryOp Op_Square = aPtr => {
            Object a = GCHandle.FromIntPtr(aPtr).Target;
            Object result = Site_Divide.Target(Site_Multiply, a, a);
            return GCHandle.ToIntPtr(GCHandle.Alloc(result));
        };

        static internal del_GenericBinOp Op_Power =
            (a, b) => GenericBinOp(Site_Power, a, b);

        static internal del_GenericUnaryOp Op_Reciprocal = aPtr => {
            Object a = GCHandle.FromIntPtr(aPtr).Target;
            Object result = Site_Divide.Target(Site_Divide, 1.0, a);
            return GCHandle.ToIntPtr(GCHandle.Alloc(result));
        };

        static internal del_GenericBinOp Op_Min = (aPtr, bPtr) => {
            Object a = GCHandle.FromIntPtr(aPtr).Target;
            Object b = GCHandle.FromIntPtr(bPtr).Target;
            Object result = (bool)Site_LessEqual.Target(Site_LessEqual, a, b) ? a : b;
            return GCHandle.ToIntPtr(GCHandle.Alloc(result));
        };

        static internal del_GenericBinOp Op_Max = (aPtr, bPtr) => {
            Object a = GCHandle.FromIntPtr(aPtr).Target;
            Object b = GCHandle.FromIntPtr(bPtr).Target;
            Object result = (bool)Site_GreaterEqual.Target(Site_GreaterEqual, a, b) ? a : b;
            return GCHandle.ToIntPtr(GCHandle.Alloc(result));
        };


        // Logical NOT - not reciprocal
        static internal del_GenericUnaryOp Op_Invert = aPtr => {
            Object a = GCHandle.FromIntPtr(aPtr).Target;
            Object result = Site_Not.Target(Site_Not, a);
            return GCHandle.ToIntPtr(GCHandle.Alloc(result));
        };

        static internal del_GenericBinOp Op_And =
            (a, b) => GenericBinOp(Site_And, a, b);
        static internal del_GenericBinOp Op_Or =
            (a, b) => GenericBinOp(Site_Or, a, b);
        static internal del_GenericBinOp Op_Xor =
            (a, b) => GenericBinOp(Site_Xor, a, b);
        static internal del_GenericBinOp Op_LShift =
            (a, b) => GenericBinOp(Site_LShift, a, b);
        static internal del_GenericBinOp Op_RShift =
            (a, b) => GenericBinOp(Site_RShift, a, b);

        // Just returns the number 1.
        static internal del_GenericUnaryOp Op_GetOne = aPtr => {
            return GCHandle.ToIntPtr(GCHandle.Alloc(1));
        };

        #endregion

    }
}
