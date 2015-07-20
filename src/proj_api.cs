/*============================================================================
 * AUTHOR: Eric G. Miller
 * DATE: 2004-09-15
 * PURPOSE: 
 *  Provide both high-level and low-level interfaces to the PROJ.4
 *  projection library.  This interface was written against a slightly
 *  modified copy of version 4.4.8.  Any significant changes were sent to
 *  upstream for incorporation into later versions.
 * COPYRIGHT: Copyright (c) 2004, 2010 California Department of Fish and Game
 * LICENSE: MIT Style
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),
 *  to deal in the Software without restriction, including without limitation
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * 	and/or sell copies of the Software, and to permit persons to whom the
 * 	Software is furnished to do so, subject to the following conditions:
 * 
 * 	The above copyright notice and this permission notice shall be included
 * 	in all copies or substantial portions of the Software.
 * 
 * 	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * 	OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * 	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
 * 	THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * 	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * 	FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * 	DEALINGS IN THE SOFTWARE.
 * 	
 *  CHANGES:
 *  For 4.7.0 (2010-01-29, Eric G. Miller)
 *      - Change definition of pj_alloc and pj_dalloc to work with IntPtr's
 *      - Change return values of pj_get_def, pj_strerrno, pj_release to
 *        IntPtr's. Use Marshal.PtrToStringAnsi. Free the IntPtr from pj_get_def
 *        using either pj_dalloc or Marshal.FreeHGlobal.  The results from
 *        the other two functions should not be released (static data).
 *      - Add GetErrorMessage and GetErrno helpers to Projection class.
 *      - Removed all unsafe code
 *      - Implemented IDisposable on the Projection class
 *      - Other minor modifications related to the changes noted above.
 *===========================================================================*/

using System;
using System.Runtime.InteropServices;

namespace ProjApi
{
	
	/// <summary>
	/// Common struct for data points
	/// </summary>
	[ StructLayout(LayoutKind.Sequential) ]
	public struct ProjUV {
		public double u;
		public double v;
	}

	/// <summary>
	/// This class thinly wraps proj.4 library functions. Some of it uses
	/// unsafe code.
	/// </summary>
	public class Proj
	{
		/// <summary>
		/// Constants for converting coordinates between radians and degrees
		/// </summary>
		public const double RAD_TO_DEG = 57.29577951308232;
		public const double DEG_TO_RAD = .0174532925199432958;
		
		/// <summary>
		/// The finder function is used by the projection library to locate
		/// resources like datum shift files and projection configuration
		/// files. (NOTE: Not functional due to calling convention).
		/// </summary>
		public delegate IntPtr FinderFunction([MarshalAs(UnmanagedType.LPStr)] string path); 

        ///// <summary>
        ///// Pointer to the global error number
        ///// </summary>
        //public unsafe static int* pj_errno = pj_get_errno_ref();

		/// <summary>
		/// Perform a forward projection (from lat/long).
		/// </summary>
		/// <param name="LP">The lat/long coordinate in radians</param>
		/// <param name="projPJ">The projection definition</param>
		/// <returns>The projected coordinate in system units</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern ProjUV pj_fwd(ProjUV LP, IntPtr projPJ);

		/// <summary>
		/// Perform an inverse projection (to lat/long).
		/// </summary>
		/// <param name="XY">The projected coordinate in system units</param>
		/// <param name="projPJ">The projection definition</param>
		/// <returns>The lat/long coordinate in radians</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern ProjUV pj_inv(ProjUV XY, IntPtr projPJ);

		/// <summary>
		/// Transform a set of coordinates from one system to another (includes datum transformation)
		/// </summary>
		/// <param name="src">Source coordinate system</param>
		/// <param name="dst">Destination coordinate system</param>
		/// <param name="point_count">Number of points in the arrays</param>
		/// <param name="point_offset">Offset to use when iterating through array. Use "1" for 
		/// normal arrays or use "2" or "3" when using a single array for all of the x, y
		/// and [optional] z elements.</param>
		/// <param name="x">The "X" coordinate array</param>
		/// <param name="y">The "Y" coordinate array</param>
		/// <param name="z">The "Z" coordinate array (may be null)</param>
		/// <returns>Zero on success, pj_errno on failure</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern int pj_transform(IntPtr src, IntPtr dst, 
			int point_count, int point_offset,
			[InAttribute, OutAttribute] double[] x,
			[InAttribute, OutAttribute] double[] y, 
			[InAttribute, OutAttribute] double[] z);

		/// <summary>
		/// Perform a datum transformation on the inputs.  Typically you would use 
		/// pj_transform which calls this function internally.
		/// </summary>
		/// <param name="src">Source coordinate system definition</param>
		/// <param name="dst">Destination coordinate system definition</param>
		/// <param name="point_count">Count of points in the array(s)</param>
		/// <param name="point_offset">Offset of each element (see pj_transform)</param>
		/// <param name="x">Array of "X" values</param>
		/// <param name="y">Array of "Y" values</param>
		/// <param name="z">Array of "Z" values (may be null)</param>
		/// <returns>Zero on success, pj_errno on failure</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern int pj_datum_transform(IntPtr src, IntPtr dst, 
			int point_count, int point_offset, 
			[InAttribute, OutAttribute] double[] x,
			[InAttribute, OutAttribute] double[] y,
			[InAttribute, OutAttribute] double[] z);

		/// <summary>
		/// Convert geocentric coordinates to geodetic.
		/// </summary>
		/// <param name="a">Ellipsoid semi-major axis</param>
		/// <param name="es">Square of ellipsoid eccentricity</param>
		/// <param name="point_count">Count of points in array(s)</param>
		/// <param name="point_offset">Offset of each element in array(s) (see pj_transform)</param>
		/// <param name="x">Array of "X" values</param>
		/// <param name="y">Array of "Y" values</param>
		/// <param name="z">Array of "Z" values</param>
		/// <returns>Zero</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern int pj_geocentric_to_geodetic(double a, double es,
			int point_count, int point_offset, 
			[InAttribute, OutAttribute] double[] x, 
			[InAttribute, OutAttribute] double[] y, 
			[InAttribute, OutAttribute] double[] z);

		/// <summary>
		/// Convert geodetic coordinates to geocentric. Called by pj_datum_transform
		/// if needed.
		/// </summary>
		/// <param name="a">Ellipsoid semi-major axis</param>
		/// <param name="es">Square of ellipsoid eccentricity</param>
		/// <param name="point_count">Count of points in array(s)</param>
		/// <param name="point_offset">Offset of each element in array(s) (see pj_transform)</param>
		/// <param name="x">Array of "X" values</param>
		/// <param name="y">Array of "Y" values</param>
		/// <param name="z">Array of "Z" values</param>
		/// <returns>Zero</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern int pj_geodetic_to_geocentric(double a, double es,
			int point_count, int point_offset, 
			[InAttribute, OutAttribute] double[] x, 
			[InAttribute, OutAttribute] double[] y, 
			[InAttribute, OutAttribute] double[] z);
		
		/// <summary>
		/// Compare the datum definitions in two coordinate system definitions
		/// for equality.
		/// </summary>
		/// <param name="srcdefn">Source coordinate system</param>
		/// <param name="dstdefn">Destination coordinate system</param>
		/// <returns>One if true, Zero if false</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern int pj_compare_datums(IntPtr srcdefn, IntPtr dstdefn );

		/// <summary>
		/// Apply a gridshift datum transformation on the inputs
		/// </summary>
		/// <param name="nadgrids">name of the gridshift file</param>
		/// <param name="inverse">flag whether shifting to or from</param>
		/// <param name="point_count">Count of points in the array(s)</param>
		/// <param name="point_offset">Offset of each element (see pj_transform)</param>
		/// <param name="x">Array of "X" values</param>
		/// <param name="y">Array of "Y" values</param>
		/// <param name="z">Array of "Z" values (may be null)</param>
		/// <returns>Zero on success, pj_errno on failure</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern int pj_apply_gridshift([MarshalAs(UnmanagedType.LPStr)] string nadgrids,
			int inverse, int point_count, int point_offset, 
			[InAttribute, OutAttribute] double[] x, 
			[InAttribute, OutAttribute] double[] y, 
			[InAttribute, OutAttribute] double[] z);

		/// <summary>
		/// Free up any loaded datum grids from memory.
		/// </summary>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void pj_deallocate_grids();
		
		/// <summary>
		/// Is the coordinate system definition lat/long ?
		/// </summary>
		/// <param name="projPJ">Coordinate system definition</param>
		/// <returns>One if true, Zero if false</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern int pj_is_latlong(IntPtr projPJ);
		
		/// <summary>
		/// Is the coordinate system definition geocentric?
		/// </summary>
		/// <param name="projPJ">Coordinate system definition</param>
		/// <returns>One if true, Zero if false</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern int pj_is_geocent(IntPtr projPJ);
		
		/// <summary>
		/// Print the coordinate system definition to stdout.
		/// </summary>
		/// <param name="projPJ">The coordinate system definition</param>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void pj_pr_list(IntPtr projPJ);

		/// <summary>
		/// Frees the memory allocated for a projection definition.
		/// Attempting to use the object after calling this function or attempting
		/// to call this function more than once with the same object will cause
		/// your application to blow up.
		/// </summary>
		/// <param name="projPJ">Opaque pointer to a projection definition (null is okay)</param>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		//public unsafe static extern void pj_free(void* projPJ);
		public static extern void pj_free(IntPtr projPJ);

		/// <summary>
		/// Install a custom function to locate resource files.  Once installed, the
		/// library will use this until uninstalled (set to null), so make sure the
		/// delegate variable is not garbage collected without "uninstalling" the
		/// function first. (NOTE: Not useable due to calling convention)
		/// </summary>
		/// <param name="f">The function delegate</param>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void pj_set_finder(
				[MarshalAs(UnmanagedType.FunctionPtr)] FinderFunction f);		

		/// <summary>
		/// Initialize a coordinate system definition like a "C" style main function
		/// loop.
		/// </summary>
		/// <param name="argc">Count of elements in argv</param>
		/// <param name="argv">Array of string parameters</param>
		/// <returns>Opaque pointer to a coordinate system definition, or null on failure.</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr pj_init(int argc, 
				[MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStr,
					 SizeParamIndex=1)] string[] argv);

		/// <summary>
		/// Initialize a projection definition object from a string of arguments
		/// </summary>
		/// <param name="pjstr">The string of projection arguments</param>
		/// <returns>Opaque pointer to a projection definition or null on failure</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr pj_init_plus(
				[MarshalAs(UnmanagedType.LPStr)] string pjstr);

		/// <summary>
		/// Get a string representation of the coordinate system definition
		/// </summary>
		/// <param name="projPJ">The coordinate system definition</param>
		/// <param name="options">Unused</param>
		/// <returns>A string representing the coordinate system definition</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr pj_get_def(IntPtr projPJ, int options);
		
		/// <summary>
		/// Return a coordinate system definition defining the lat/long coordinate        */
		/// system on which a projection is based.  If the coordinate
		/// system passed in is latlong, a clone of the same will be returned.                   
		/// </summary>
		/// <param name="projPJ">The source coordinate system definition</param>
		/// <returns>The lat/long coordinate system definition</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr pj_latlong_from_proj(IntPtr projPJ);
		
		/// <summary>
		/// Allocate a chunk of memory using malloc.
		/// </summary>
		/// <param name="size">The size of the memory chunk to allocate</param>
		/// <returns>A pointer to the allocated memory, or null on failure.</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr pj_malloc(uint size);

		/// <summary>
		/// Deallocate a chunk of memory previously allocated with pj_alloc (malloc).
		/// </summary>
		/// <param name="memory">The pointer to the chunk of memory to free.</param>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void pj_dalloc(IntPtr memory);

		/// <summary>
		/// Get the string value corresponding to the error number.
		/// </summary>
		/// <param name="errno">The error number</param>
		/// <returns>The error message</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr pj_strerrno(int errno);

		/// <summary>
		/// Get a pointer to the int holding the last error number
		/// </summary>
		/// <returns>pointer to the error number variable</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr pj_get_errno_ref();

		/// <summary>
		/// Get the PROJ.4 library release string
		/// </summary>
		/// <returns>string containing library release version</returns>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr pj_get_release();

		/// <summary>
		/// Specifies directories in which the projection library should look for
		/// resource files.
		/// </summary>
		/// <param name="count">number of elements in the array</param>
		/// <param name="path">array of strings specifying directories to look for files in.</param>
		[DllImport("proj.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void pj_set_searchpath(int count, string[] path );
	}

	/// <summary>
	/// This class is a thicker interface to the PROJ.4 library.  It exposes a small 
	/// set of methods, but generally these are all that is needed.
	/// </summary>
	public class Projection : IDisposable {

		// INTERNAL data
		/// <summary>
		/// The pointer to the projection definition object
		/// </summary>
		internal IntPtr prj = IntPtr.Zero;
		/// <summary>
		/// Cache of the definition string returned by pj_get_def
		/// </summary>
		internal string out_def = null;

		/// <summary>
		/// Common object initialization function
		/// </summary>
		/// <param name="definition">The projection definition string</param>
		/// <exception cref="System.ArgumentException">Thrown when initialization fails.  
		/// The reason may vary and will be documented in the Message</exception>
		private void Initialize(string definition) {
			IntPtr thePrj = Proj.pj_init_plus(definition);
			if (thePrj == IntPtr.Zero) {
                string message = GetErrorMessage(GetErrNo());
				throw new System.ArgumentException(message);
			}
			this.prj = thePrj;
			this.out_def = null;
		}

        /// <summary>
        /// Read the current pj_errno value.
        /// </summary>
        /// <returns>The current pj_errno value.</returns>
        public static int GetErrNo()
        {
            int errno = 0;
            IntPtr pErrNo = Proj.pj_get_errno_ref();
            errno = Marshal.ReadInt32(pErrNo);
            return errno;
        }

        /// <summary>
        /// Get the error message corresponding to
        /// the errno
        /// </summary>
        /// <param name="errno">The error number</param>
        /// <returns>The message, or null if errno == 0</returns>
        public static string GetErrorMessage(int errno)
        {
            if (errno == 0) return null;
            IntPtr pMsg = Proj.pj_strerrno(errno);
            return Marshal.PtrToStringAnsi(pMsg);
        }

		/// <summary>
		/// Instance version checks initialization status.
		/// </summary>
		private void CheckInitialized() {
			Projection.CheckInitialized(this);
		}

		/// <summary>
		/// Static version that checks initialization status.
		/// </summary>
		/// <param name="p">The projection object</param>
		private static void CheckInitialized(Projection p) {
			if (p.prj == IntPtr.Zero) {
				throw new ApplicationException("Projection not initialized");
			}
		}

		/// <summary>
		/// The default constructor
		/// </summary>
		public Projection() {}

		/// <summary>
		/// Constructor with a definition
		/// </summary>
		/// <param name="paramaters">string defining the coordinate system</param>
		public Projection(string definition) : base() {
			this.Initialize(definition);
		}

		// PROPERTIES

		/// <summary>
		/// A string representing the coordinate system. Setting it [re]initializes the
		/// projection definition.
		/// </summary>
		/// <exception cref="System.ArgumentException">Thrown when initialization fails (set).  
		/// The reason may vary and will be documented in the Message</exception>
		/// <exception cref="System.ApplicationException">Thrown when the projection is
		/// not initialized (get).</exception>
		public string Definition {
			set { this.Initialize(value); }
			get {
				this.CheckInitialized();
                if (this.out_def == null)
                {
                    IntPtr pDef = Proj.pj_get_def(this.prj, 0);
                    this.out_def = Marshal.PtrToStringAnsi(pDef);
                    Proj.pj_dalloc(pDef);
                }
				return this.out_def;
			}
		}

		/// <summary>
		/// Returns true if the projection definition is Lat/Long.
		/// </summary>
		/// <exception cref="System.ApplicationException">Thrown when the projection is
		/// not initialized (get).</exception>
		public bool IsLatLong {
			get { 
				this.CheckInitialized();
				return (Proj.pj_is_latlong(this.prj) == 0) ? false : true; 
			}
		}

		/// <summary>
		/// Returns true if the projection definition is Geocentric (XYZ)
		/// </summary>
		/// <exception cref="System.ApplicationException">Thrown when the projection is
		/// not initialized (get).</exception>
		public bool IsGeoCentric {
			get {
				this.CheckInitialized();
				return (Proj.pj_is_geocent(this.prj) == 0) ? false : true;
			}
		}

		// METHODS

		/// <summary>
		/// Get a projection object with the underlying
		/// Lat/Long definition
		/// </summary>
		/// <returns>Projection</returns>
		/// <exception cref="System.ApplicationException">Thrown when the projection is
		/// not initialized.</exception>
		/// <exception cref="System.ArgumentException">Thrown when the underlying library
		/// does not return a valid Lat/Long projection object.  This might happen if the
		/// original projection does not have an underlying Lat/Long coordinate system.
		/// </exception>
		public Projection GetLatLong() {
			this.CheckInitialized();
			Projection new_prj = new Projection();
			new_prj.prj = Proj.pj_latlong_from_proj(this.prj);
			if (new_prj.prj == IntPtr.Zero) {
				string message = GetErrorMessage(GetErrNo());
				throw new System.ArgumentException(message);
			}
			return new_prj;
		}

		/// <summary>
		/// Returns the projection definition string (Same as .Definition property)
		/// </summary>
		/// <returns>Projection definition string</returns>
		/// <exception cref="System.ApplicationException">Thrown when the projection is
		/// not initialized.</exception>
		public override string ToString() {
			return this.Definition;
		}

		/// <summary>
		/// Sets search directories for the PROJ.4 library to look for its resource
		/// files (such as datum grid files, state plane files, etc...).  Search
		/// paths are only used if other means fail (default install directory, PROJ_LIB
		/// environment variable, installed callback, current directory).  Therefore,
		/// do not expect the search path to override the other methods of specifying
		/// the location of resource files.
		/// </summary>
		/// <param name="path">An array of strings specifying directories to look for
		/// files in.</param>
		public static void SetSearchPath(string[] path) {
			if (path != null && path.Length > 0)
				Proj.pj_set_searchpath(path.Length, path);
		}

		/// <summary>
		/// Transform coordinates from one projection system to another
		/// </summary>
		/// <param name="dst">The destination projection</param>
		/// <param name="x">The "X" coordinate values.</param>
		/// <param name="y">The "Y" coordinate values.</param>
		/// <exception cref="System.ApplicationException">Thrown when the projection is
		/// not initialized or the transformation failed.  The message will indicate the error.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// May be thrown for any of the following reasons:
		/// <list type="bullet">
		/// <item>The "x" array is null</item>
		/// <item>The "y" array is null</item>
		/// <item>The length of the x and y arrays don't match</item>
		/// </list>
		/// </exception>
		public void Transform (Projection dst, double[] x, double[] y) {
			this.Transform(dst, x, y, null);
		}

		/// <summary>
		/// Transform coordinates from one projection system to another
		/// </summary>
		/// <param name="dst">The destination projection</param>
		/// <param name="x">The "X" coordinate values.</param>
		/// <param name="y">The "Y" coordinate values.</param>
		/// <param name="z">The "Z" coordinate values.</param>
		/// <exception cref="System.ApplicationException">Thrown when the projection is
		/// not initialized or the transformation failed.  The message will indicate the error.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// May be thrown for any of the following reasons:
		/// <list type="bullet">
		/// <item>The "x" array is null</item>
		/// <item>The "y" array is null</item>
		/// <item>The length of the x, y and z (if not null) arrays don't match</item>
		/// </list>
		/// </exception>
		public void Transform (Projection dst, double[] x, double[] y, double[] z) {
			Projection.Transform(this, dst, x, y, z);
		}

		/// <summary>
		/// Transform coordinates from one projection system to another
		/// </summary>
		/// <param name="src">The source projection</param>
		/// <param name="dst">The destination projection</param>
		/// <param name="x">The "X" coordinate values.</param>
		/// <param name="y">The "Y" coordinate values.</param>
		/// <exception cref="System.ApplicationException">Thrown when the projection is
		/// not initialized or the transformation failed.  The message will indicate the error.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// May be thrown for any of the following reasons:
		/// <list type="bullet">
		/// <item>The "x" array is null</item>
		/// <item>The "y" array is null</item>
		/// <item>The length of the x and y arrays don't match</item>
		/// </list>
		/// </exception>
		public static void Transform (Projection src, Projection dst,
										double[] x, double[] y) {
			Projection.Transform(src, dst, x, y, null);
		}

		/// <summary>
		/// Transform coordinates from one projection system to another
		/// </summary>
		/// <param name="src">The source projection</param>
		/// <param name="dst">The destination projection</param>
		/// <param name="x">The "X" coordinate values.</param>
		/// <param name="y">The "Y" coordinate values.</param>
		/// <param name="z">The "Z" coordinate values.</param>
		/// <exception cref="System.ApplicationException">Thrown when the projection is
		/// not initialized or the transformation failed.  The message will indicate the error.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// May be thrown for any of the following reasons:
		/// <list type="bullet">
		/// <item>The "x" array is null</item>
		/// <item>The "y" array is null</item>
		/// <item>The length of the x, y and z (if not null) arrays don't match</item>
		/// </list>
		/// </exception>
		public static void Transform (Projection src, Projection dst,
				double[] x, double[] y, double[] z) {
			Projection.CheckInitialized(src);
			Projection.CheckInitialized(dst);
			if (x == null) {
				throw new ArgumentException("Argument is required", "x");
			}
			if (y == null) {
				throw new ArgumentException("Argument is required", "y");
			}
			if (x.Length != y.Length || (z != null && z.Length != x.Length)) {
				throw new ArgumentException("Coordinate arrays must have the same length");
			}
			if (src.IsLatLong) {
				for (int i = 0; i < x.Length; i++) {
					x[i] *= Proj.DEG_TO_RAD;
					y[i] *= Proj.DEG_TO_RAD;
				}
			}
			int result = Proj.pj_transform(src.prj, dst.prj, x.Length, 1, x, y, z);
			if (result != 0) {
                string message = "Tranformation Error";
                int errno = GetErrNo();
                if (errno != 0)
                    message = Projection.GetErrorMessage(errno);
				throw new ApplicationException(message);
			}
			if (dst.IsLatLong) {
				for (int i = 0; i < x.Length; i++) {
					x[i] *= Proj.RAD_TO_DEG;
					y[i] *= Proj.RAD_TO_DEG;
				}
			}
		}


        #region IDisposable Members

        public void Dispose()
        {
            if (this.prj != IntPtr.Zero)
                Proj.pj_free(this.prj);
        }

        #endregion
    }
}
