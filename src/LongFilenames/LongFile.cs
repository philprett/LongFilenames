﻿using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LongFilenames
{
	internal static class LongFile
	{
		//private const int MAX_PATH = 260;
		private const int MAX_PATH = 0;

		public static bool Exists(string path)
		{
			if (path.Length < MAX_PATH) return System.IO.File.Exists(path);
			var attr = NativeMethods.GetFileAttributesW(GetWin32LongPath(path));
			return attr != NativeMethods.INVALID_FILE_ATTRIBUTES;
		}

		public static void Delete(string path)
		{
			if (path.Length < MAX_PATH) System.IO.File.Delete(path);
			else
			{
				bool ok = NativeMethods.DeleteFileW(GetWin32LongPath(path));
				if (!ok) ThrowWin32Exception();
			}
		}

		public static void AppendAllText(string path, string contents)
		{
			AppendAllText(path, contents, Encoding.Default);
		}

		public static void AppendAllText(string path, string contents, Encoding encoding)
		{
			if (path.Length < MAX_PATH)
			{
				System.IO.File.AppendAllText(path, contents, encoding);
			}
			else
			{
				var fileHandle = CreateFileForAppend(GetWin32LongPath(path));
				using (var fs = new System.IO.FileStream(fileHandle, System.IO.FileAccess.Write))
				{
					var bytes = encoding.GetBytes(contents);
					fs.Position = fs.Length;
					fs.Write(bytes, 0, bytes.Length);
				}
			}
		}

		public static void WriteAllText(string path, string contents)
		{
			WriteAllText(path, contents, Encoding.Default);
		}

		public static void WriteAllText(string path, string contents, Encoding encoding)
		{
			if (path.Length < MAX_PATH)
			{
				System.IO.File.WriteAllText(path, contents, encoding);
			}
			else
			{
				var fileHandle = CreateFileForWrite(GetWin32LongPath(path));

				using (var fs = new System.IO.FileStream(fileHandle, System.IO.FileAccess.Write))
				{
					var bytes = encoding.GetBytes(contents);
					fs.Write(bytes, 0, bytes.Length);
				}
			}
		}

		public static void WriteAllBytes(string path, byte[] bytes)
		{
			if (path.Length < MAX_PATH)
			{
				System.IO.File.WriteAllBytes(path, bytes);
			}
			else
			{
				var fileHandle = CreateFileForWrite(GetWin32LongPath(path));

				using (var fs = new System.IO.FileStream(fileHandle, System.IO.FileAccess.Write))
				{
					fs.Write(bytes, 0, bytes.Length);
				}
			}
		}

		public static void Copy(string sourceFileName, string destFileName)
		{
			Copy(sourceFileName, destFileName, false);
		}

		public static void Copy(string sourceFileName, string destFileName, bool overwrite)
		{
			if (sourceFileName.Length < MAX_PATH && (destFileName.Length < MAX_PATH)) System.IO.File.Copy(sourceFileName, destFileName, overwrite);
			else
			{
				var ok = NativeMethods.CopyFileW(GetWin32LongPath(sourceFileName), GetWin32LongPath(destFileName), !overwrite);
				if (!ok) ThrowWin32Exception();
			}
		}

		public static void Move(string sourceFileName, string destFileName)
		{
			if (sourceFileName.Length < MAX_PATH && (destFileName.Length < MAX_PATH)) System.IO.File.Move(sourceFileName, destFileName);
			else
			{
				var ok = NativeMethods.MoveFileW(GetWin32LongPath(sourceFileName), GetWin32LongPath(destFileName));
				if (!ok) ThrowWin32Exception();
			}
		}

		public static string ReadAllText(string path)
		{
			return ReadAllText(path, Encoding.Default);
		}

		public static string ReadAllText(string path, Encoding encoding)
		{
			if (path.Length < MAX_PATH) { return System.IO.File.ReadAllText(path, encoding); }
			var fileHandle = GetFileHandle(GetWin32LongPath(path));

			using (var fs = new System.IO.FileStream(fileHandle, System.IO.FileAccess.Read))
			{
				var data = new byte[fs.Length];
				fs.Read(data, 0, data.Length);
				return encoding.GetString(data);
			}
		}

		public static string[] ReadAllLines(string path)
		{
			return ReadAllLines(path, Encoding.Default);
		}

		public static string[] ReadAllLines(string path, Encoding encoding)
		{
			if (path.Length < MAX_PATH) { return System.IO.File.ReadAllLines(path, encoding); }
			var fileHandle = GetFileHandle(GetWin32LongPath(path));

			using (var fs = new System.IO.FileStream(fileHandle, System.IO.FileAccess.Read))
			{
				var data = new byte[fs.Length];
				fs.Read(data, 0, data.Length);
				var str = encoding.GetString(data);
				if (str.Contains("\r")) return str.Split(new[] { "\r\n" }, StringSplitOptions.None);
				return str.Split('\n');
			}
		}
		public static byte[] ReadAllBytes(string path)
		{
			if (path.Length < MAX_PATH) return System.IO.File.ReadAllBytes(path);
			var fileHandle = GetFileHandle(GetWin32LongPath(path));

			using (var fs = new System.IO.FileStream(fileHandle, FileAccess.Read))
			{
				var data = new byte[fs.Length];
				fs.Read(data, 0, data.Length);
				return data;
			}
		}

		public static System.IO.FileAttributes GetAttributes(string path)
		{
			if (path.Length < MAX_PATH)
			{
				return System.IO.File.GetAttributes(path);
			}
			else
			{
				var longFilename = GetWin32LongPath(path);
				return (System.IO.FileAttributes)NativeMethods.GetFileAttributesW(longFilename);
			}
		}

		public static void SetAttributes(string path, System.IO.FileAttributes attributes)
		{
			if (path.Length < MAX_PATH)
			{
				System.IO.File.SetAttributes(path, attributes);
			}
			else
			{
				var longFilename = GetWin32LongPath(path);
				NativeMethods.SetFileAttributesW(longFilename, (int)attributes);
			}
		}

		#region Helper methods

		private static SafeFileHandle CreateFileForWrite(string filename)
		{
			if (filename.Length >= MAX_PATH) filename = GetWin32LongPath(filename);
			SafeFileHandle hfile = NativeMethods.CreateFile(filename, (int)NativeMethods.FILE_GENERIC_WRITE, NativeMethods.FILE_SHARE_NONE, IntPtr.Zero, NativeMethods.CREATE_ALWAYS, 0, IntPtr.Zero);
			if (hfile.IsInvalid) ThrowWin32Exception();
			return hfile;
		}

		private static SafeFileHandle CreateFileForAppend(string filename)
		{
			if (filename.Length >= MAX_PATH) filename = GetWin32LongPath(filename);
			SafeFileHandle hfile = NativeMethods.CreateFile(filename, (int)NativeMethods.FILE_GENERIC_WRITE, NativeMethods.FILE_SHARE_NONE, IntPtr.Zero, NativeMethods.CREATE_NEW, 0, IntPtr.Zero);
			if (hfile.IsInvalid)
			{
				hfile = NativeMethods.CreateFile(filename, (int)NativeMethods.FILE_GENERIC_WRITE, NativeMethods.FILE_SHARE_NONE, IntPtr.Zero, NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero);
				if (hfile.IsInvalid) ThrowWin32Exception();
			}
			return hfile;
		}

		internal static SafeFileHandle GetFileHandle(string filename)
		{
			if (filename.Length >= MAX_PATH) filename = GetWin32LongPath(filename);
			SafeFileHandle hfile = NativeMethods.CreateFile(filename, (int)NativeMethods.FILE_GENERIC_READ, NativeMethods.FILE_SHARE_READ, IntPtr.Zero, NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero);
			if (hfile.IsInvalid) ThrowWin32Exception();
			return hfile;
		}

		internal static SafeFileHandle GetFileHandleWithWrite(string filename)
		{
			if (filename.Length >= MAX_PATH) filename = GetWin32LongPath(filename);
			SafeFileHandle hfile = NativeMethods.CreateFile(filename, (int)(NativeMethods.FILE_GENERIC_READ | NativeMethods.FILE_GENERIC_WRITE | NativeMethods.FILE_WRITE_ATTRIBUTES), NativeMethods.FILE_SHARE_NONE, IntPtr.Zero, NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero);
			if (hfile.IsInvalid) ThrowWin32Exception();
			return hfile;
		}

		public static System.IO.FileStream GetFileStream(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read)
		{
			var longFilename = GetWin32LongPath(filename);
			SafeFileHandle hfile;
			if (access == FileAccess.Write)
			{
				hfile = NativeMethods.CreateFile(longFilename, (int)(NativeMethods.FILE_GENERIC_READ | NativeMethods.FILE_GENERIC_WRITE | NativeMethods.FILE_WRITE_ATTRIBUTES), NativeMethods.FILE_SHARE_NONE, IntPtr.Zero, NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero);
			}
			else
			{
				hfile = NativeMethods.CreateFile(longFilename, (int)NativeMethods.FILE_GENERIC_READ, NativeMethods.FILE_SHARE_READ, IntPtr.Zero, NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero);
			}

			if (hfile.IsInvalid) ThrowWin32Exception();

			return new System.IO.FileStream(hfile, access);
		}


		public static void ThrowWin32Exception()
		{
			int code = Marshal.GetLastWin32Error();
			if (code != 0)
			{
				throw new System.ComponentModel.Win32Exception(code);
			}
		}

		public static string GetWin32LongPath(string path)
		{
			if (path.StartsWith(@"\\?\")) return path;

			if (path.StartsWith("\\"))
			{
				path = @"\\?\UNC\" + path.Substring(2);
			}
			else if (path.Contains(":"))
			{
				path = @"\\?\" + path;
			}
			else
			{
				var currdir = Environment.CurrentDirectory;
				path = Combine(currdir, path);
				while (path.Contains("\\.\\")) path = path.Replace("\\.\\", "\\");
				path = @"\\?\" + path;
			}
			return path.TrimEnd('.'); ;
		}

		public static string Combine(string path1, string path2)
		{
			return path1.TrimEnd('\\') + "\\" + path2.TrimStart('\\').TrimEnd('.'); ;
		}


		#endregion

		public static void SetCreationTime(string path, DateTime creationTime)
		{
			long cTime = 0;
			long aTime = 0;
			long wTime = 0;

			using (var handle = GetFileHandleWithWrite(path))
			{
				NativeMethods.GetFileTime(handle, ref cTime, ref aTime, ref wTime);
				var fileTime = creationTime.ToFileTimeUtc();
				if (!NativeMethods.SetFileTime(handle, ref fileTime, ref aTime, ref wTime))
				{
					throw new Win32Exception();
				}
			}
		}

		public static void SetLastAccessTime(string path, DateTime lastAccessTime)
		{
			long cTime = 0;
			long aTime = 0;
			long wTime = 0;

			using (var handle = GetFileHandleWithWrite(path))
			{
				NativeMethods.GetFileTime(handle, ref cTime, ref aTime, ref wTime);

				var fileTime = lastAccessTime.ToFileTimeUtc();
				if (!NativeMethods.SetFileTime(handle, ref cTime, ref fileTime, ref wTime))
				{
					throw new Win32Exception();
				}
			}
		}

		public static void SetLastWriteTime(string path, DateTime lastWriteTime)
		{
			long cTime = 0;
			long aTime = 0;
			long wTime = 0;

			using (var handle = GetFileHandleWithWrite(path))
			{
				NativeMethods.GetFileTime(handle, ref cTime, ref aTime, ref wTime);

				var fileTime = lastWriteTime.ToFileTimeUtc();
				if (!NativeMethods.SetFileTime(handle, ref cTime, ref aTime, ref fileTime))
				{
					throw new Win32Exception();
				}
			}
		}

		public static DateTime GetLastWriteTime(string path)
		{
			long cTime = 0;
			long aTime = 0;
			long wTime = 0;

			using (var handle = GetFileHandle(path))
			{
				NativeMethods.GetFileTime(handle, ref cTime, ref aTime, ref wTime);

				return DateTime.FromFileTimeUtc(wTime);
			}
		}

		public static string GetParent(string path)
		{
			if (!path.Contains("\\"))
			{
				return string.Empty;
			}
			else
			{
				return path.Substring(0, path.LastIndexOf("\\"));
			}
		}

		public static string GetName(string path)
		{
			if (!path.Contains("\\"))
			{
				return path;
			}
			else
			{
				return path.Substring(path.LastIndexOf("\\") + 1);
			}
		}

		public static string GetNameWithoutExtension(string path)
		{
			string name = GetName(path);
			int dot = name.IndexOf(".");
			if (dot == 0)
			{
				return name;
			}
			else if (dot > 0)
			{
				return name.Substring(0, name.LastIndexOf("."));
			}
			else
			{
				return name;
			}
		}

		public static string GetExtension(string path)
		{
			string name = GetName(path);
			if (!name.Contains("."))
			{
				return string.Empty;
			}
			else
			{
				return name.Substring(name.LastIndexOf("."));
			}
		}

		public static long GetFileSize(string path)
		{
			System.IO.FileInfo fi = new System.IO.FileInfo(path);
			return fi.Length;
		}

	}

}
