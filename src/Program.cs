using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ProjApi;
using Mono.Options;
using FileHelpers;

namespace convert_bev_address_data {


	class Program {

		static void Main( string[] args ) {

			string inputDir = string.Empty;
			string csvOut = string.Empty;
			string targetEpsg = "31287";
			bool _ShowUsage = false;
			//bool _Verbose = false;
			int decimalPlaces = 6;

			string decimalsMsg = @"decimal places, optional (default: 6)
depending on the chosen output crs it makes sense to limit the number of decimal places.
e.g. 6 decimal places don't make sense for crs with [m] units and just necessarly bloat the resulting csv.
But it makes sense to have 6 decimal places with EPSG:4326.";



			var p = new OptionSet() {
				{"i|input=", "diretory containing extracted CSVs", v=>inputDir=v },
				{"o|output=", "output file, optional (default: out.csv)", v=>csvOut=v },
				{"e|epsg=", "target epsg code, optional (default: 31287)", v=>targetEpsg=v },
				{"d|decimals=", decimalsMsg, v=> {int dp; if(int.TryParse(v,out dp)) {decimalPlaces=dp; } }                },
				//{"v|verbose", "verbose output, optional", v=>_Verbose=v!=null },
				{"u|usage","show this message and exit", v => _ShowUsage = v != null }
			};

			List<string> extra;
			try {
				extra = p.Parse( args );
			}
			catch (OptionException e) {
				Console.WriteLine( e.Message );
				Console.WriteLine( "try 'convert-bev-address-data --usage'" );
				return;
			}

			if (args.Length < 1 || string.IsNullOrWhiteSpace( inputDir ) || _ShowUsage) {
				ShowUsage( p );
				return;
			}

			string csvAddress = Path.Combine( inputDir, "ADRESSE.CSV" );
			string csvStreet = Path.Combine( inputDir, "STRASSE.CSV" );
			string csvGemeinde = Path.Combine( inputDir, "GEMEINDE.CSV" );
			string csvOrt = Path.Combine( inputDir, "ORTSCHAFT.CSV" );
			if (!filesExist( csvAddress, csvStreet, csvGemeinde )) {
				return;
			}

			if (string.IsNullOrWhiteSpace( csvOut )) {
				csvOut = Path.Combine( inputDir, "out.csv" );
			} else {
				string outPath = Path.GetFullPath( csvOut );
				if (!string.IsNullOrWhiteSpace( outPath ) && !Directory.Exists( outPath )) {
					Console.Write( "output directory does not exists: {0}", outPath );
				}
			}

			Projection prjTarget = null;
			try {
				prjTarget = new Projection( "+init=epsg:" + targetEpsg );
			}
			catch (Exception ex) {
				Console.WriteLine( "could not initialize EPSG:" + targetEpsg );
				Console.WriteLine( ex.Message );
				return;
			}
			if (null == targetEpsg) {
				Console.WriteLine( "could not initialize EPSG:" + targetEpsg );
				return;
			}
			Console.WriteLine( "output crs, EPSG:" + targetEpsg );

			char[] delimiter = ";".ToCharArray();
			CultureInfo enUS = new CultureInfo( "en-US" );
			enUS.NumberFormat.NumberGroupSeparator = string.Empty;
			string decimalPlacesTxt = string.Format( "N{0}", decimalPlaces );

			Console.WriteLine( "reading street names" );
			Dictionary<string, string> streets = new Dictionary<string, string>();
			using (TextReader tr = new StreamReader( csvStreet )) {
				tr.ReadLine();
				string line;
				while (!string.IsNullOrWhiteSpace( line = tr.ReadLine() )) {
					string[] tokens = line.Split( delimiter );
					string skz = tokens[0].Replace( "\"", string.Empty );
					string name = tokens[1].Replace( "\"", string.Empty );
					streets.Add( skz, name );
				}
			}


			Console.WriteLine( "reading orte" );
			Dictionary<string, string> orte = new Dictionary<string, string>();
			using (TextReader tr = new StreamReader( csvOrt )) {
				tr.ReadLine();
				string line;
				while (!string.IsNullOrWhiteSpace( line = tr.ReadLine() )) {
					string[] tokens = line.Split( delimiter );
					string okz = tokens[1].Replace( "\"", string.Empty );
					string name = tokens[2].Replace( "\"", string.Empty );
					orte.Add( okz, name );
				}
			}


			Console.WriteLine( "reading gemeinden" );
			Dictionary<string, string> gemeinden = new Dictionary<string, string>();
			using (TextReader tr = new StreamReader( csvGemeinde )) {
				tr.ReadLine();
				string line;
				while (!string.IsNullOrWhiteSpace( line = tr.ReadLine() )) {
					string[] tokens = line.Split( delimiter );
					string gkz = tokens[0].Replace( "\"", string.Empty );
					string name = tokens[1].Replace( "\"", string.Empty );
					gemeinden.Add( gkz, name );
				}
			}


			Dictionary<string, Projection> srcProjs = new Dictionary<string, Projection>();
			//wrong towgs84 paramters
			//srcProjs.Add( "31254", new Projection( "+init=epsg:31254" ) );
			//srcProjs.Add( "31254", new Projection( "+init=epsg:31254" ) );
			//srcProjs.Add( "31254", new Projection( "+init=epsg:31254" ) );
			srcProjs.Add( "31254", new Projection( "+proj=tmerc +lat_0=0 +lon_0=10.33333333333333 +k=1 +x_0=0 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs" ) );
			srcProjs.Add( "31255", new Projection( "+proj=tmerc +lat_0=0 +lon_0=13.33333333333333 +k=1 +x_0=0 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs" ) );
			srcProjs.Add( "31256", new Projection( "+proj=tmerc +lat_0=0 +lon_0=16.33333333333333 +k=1 +x_0=0 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs " ) );


			Console.WriteLine( "reading addresses" );

			var fileHelper = new FileHelperAsyncEngine<BevAddress>();
			long skipCnt = 0;

			using (fileHelper.BeginReadFile( csvAddress )) {

				using (TextWriter tw = new StreamWriter( csvOut, false, Encoding.UTF8 )) {

					tw.WriteLine( "gemeinde;plz;ort;strasse;hnr;x-{0};y-{0}", targetEpsg );
					long lineCnter = 1; //start with 1 (skipped header)
					foreach (var record in fileHelper) {

						lineCnter++;

						if (!record.RW.HasValue || !record.HW.HasValue) {
							skipCnt++;
							Console.WriteLine( "no coordinates: {0}", recordToString( record, streets ) );
							continue;
						}

						double[] x = new double[] { record.RW.Value };
						double[] y = new double[] { record.HW.Value };

						try {
							srcProjs[record.EPSG].Transform( prjTarget, x, y );
						}
						catch (Exception ex2) {
							Console.WriteLine( "{1}======ERROR: prj transform failed! line {0}:", lineCnter, Environment.NewLine );
							Console.WriteLine( recordToString( record, streets ) );
							Console.WriteLine( ex2 );
							Console.WriteLine( Environment.NewLine );
							continue;
						}

						string outline = string.Format(
							enUS
							, "{0};{1};{2};{3};{4};{5};{6}"
							, gemeinden.ContainsKey( record.GKZ ) ? gemeinden[record.GKZ] : string.Empty
							, record.PLZ
							, orte.ContainsKey( record.OKZ ) ? orte[record.OKZ] : string.Empty
							, streets.ContainsKey( record.SKZ ) ? streets[record.SKZ] : string.Empty
							, constructHouseNumber( record )
							, x[0].ToString( decimalPlacesTxt, enUS )
							, y[0].ToString( decimalPlacesTxt, enUS )
						);

						tw.WriteLine( outline );
					}
				}
			}

			//clean up
			Console.WriteLine( "skipped: {0}{1}{0}", Environment.NewLine, skipCnt );
			prjTarget.Dispose();
			prjTarget = null;
			srcProjs["31254"].Dispose();
			srcProjs["31254"] = null;
			srcProjs["31255"].Dispose();
			srcProjs["31255"] = null;
			srcProjs["31256"].Dispose();
			srcProjs["31256"] = null;

		}


		private static string constructHouseNumber( BevAddress record ) {

			List<string> hnr = new List<string>();
			if (!string.IsNullOrEmpty( record.HAUSNRTEXT )) { hnr.Add( record.HAUSNRTEXT ); }
			if (record.HAUSNRZAHL1.HasValue) { hnr.Add( record.HAUSNRZAHL1.ToString() ); }
			if (!string.IsNullOrEmpty( record.HAUSNRBUCHSTABE1 )) { hnr.Add( record.HAUSNRBUCHSTABE1 ); }
			if (!string.IsNullOrEmpty( record.HAUSNRVERBINDUNG1 )) { hnr.Add( record.HAUSNRVERBINDUNG1 ); }
			if (record.HAUSNRZAHL2.HasValue) { hnr.Add( record.HAUSNRZAHL2.ToString() ); }
			if (!string.IsNullOrEmpty( record.HAUSNRBUCHSTABE2 )) { hnr.Add( record.HAUSNRBUCHSTABE2 ); }

			return string.Join( " ", hnr.ToArray() );
		}


		private static string recordToString( BevAddress record, Dictionary<string, string> streets ) {
			return string.Format(
				"adrcd:{0} gkz:{1} okz:{2} plz:{3} {4}"
				, record.ADRCD
				, record.GKZ
				, record.OKZ
				, record.PLZ
				, streets.ContainsKey( record.SKZ ) ? streets[record.SKZ] : string.Empty
			);
		}


		private static void ShowUsage( OptionSet p ) {
			Console.WriteLine( "usage:" );
			Console.WriteLine( "      convert-bev-address-data -i <PATH-TO-BEV-DATA-DIRECTORY> [-o] [-e] [-d]" );
			Console.WriteLine();
			Console.WriteLine( "options:" );
			p.WriteOptionDescriptions( Console.Out );
		}


		private static bool filesExist( params string[] files ) {

			bool retVal = true;

			foreach (var file in files) {
				if (!File.Exists( file )) {
					Console.WriteLine( "not found: {0}", file );
					retVal = false;
				}
			}

			return retVal;
		}



	}
}
