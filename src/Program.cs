using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ProjApi;
using Mono.Options;


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

			using (TextReader tr = new StreamReader( csvAddress )) {

				//skip header
				tr.ReadLine();

				string line;
				Int64 lineCnter = 1;

				using (TextWriter tw = new StreamWriter( csvOut, false, Encoding.UTF8 )) {

					tw.WriteLine( "gemeinde;plz;ort;strasse;hnr;x-{0};y-{0}", targetEpsg );
					while (!string.IsNullOrWhiteSpace( line = tr.ReadLine() )) {

						lineCnter++;
						try {
							string[] tokens = line.Split( delimiter );

							string adrcd = tokens[1].Replace( "\"", string.Empty );
							string gkz = tokens[1].Replace( "\"", string.Empty );
							string okz = tokens[2].Replace( "\"", string.Empty );
							string plz = tokens[3].Replace( "\"", string.Empty );
							string skz = tokens[4].Replace( "\"", string.Empty );
							string hnrtxt = tokens[6].Replace( "\"", string.Empty );
							string hnrzahl1 = tokens[7].Replace( "\"", string.Empty );
							string hnrchar1 = tokens[8].Replace( "\"", string.Empty );
							string hnrverb = tokens[9].Replace( "\"", string.Empty );
							string hnrzahl2 = tokens[10].Replace( "\"", string.Empty );
							string hnrchar2 = tokens[11].Replace( "\"", string.Empty );
							string epsg = tokens[17].Replace( "\"", string.Empty );
							string xTxt = tokens[15].Replace( "\"", string.Empty );
							string yTxt = tokens[16].Replace( "\"", string.Empty );

							if (string.IsNullOrWhiteSpace( xTxt ) || string.IsNullOrWhiteSpace( yTxt )) {
								Console.WriteLine( "skipped, no coordinates, adrcd:{0} gkz:{1} okz:{2} plz:{3} {4}", adrcd, gkz, okz, plz, streets.ContainsKey( skz ) ? streets[skz] : string.Empty );
								continue;
							}

							double[] x = new double[] { Convert.ToDouble( xTxt, enUS ) };
							double[] y = new double[] { Convert.ToDouble( yTxt, enUS ) };

							try {
								srcProjs[epsg].Transform( prjTarget, x, y );
							}
							catch (Exception ex2) {
								Console.WriteLine( "{1}======ERROR: prj transform failed! line {0}:", lineCnter, Environment.NewLine );
								Console.WriteLine( line );
								Console.WriteLine( ex2 );
								Console.WriteLine( Environment.NewLine );
								continue;
							}

							string outline = string.Format(
								enUS
								, "{0};{1};{2};{3};{4}{5}{6}{7}{8}{9};{10};{11}"
								, gemeinden.ContainsKey( gkz ) ? gemeinden[gkz] : string.Empty
								, plz
								, orte.ContainsKey( okz ) ? orte[okz] : string.Empty
								, streets.ContainsKey( skz ) ? streets[skz] : string.Empty
								, !string.IsNullOrEmpty( hnrtxt ) ? " " + hnrtxt : string.Empty
								, !string.IsNullOrEmpty( hnrzahl1 ) ? " " + hnrzahl1 : string.Empty
								, !string.IsNullOrEmpty( hnrchar1 ) ? " " + hnrchar1 : string.Empty
								, !string.IsNullOrEmpty( hnrverb ) ? " " + hnrverb : string.Empty
								, !string.IsNullOrEmpty( hnrzahl2 ) ? " " + hnrzahl2 : string.Empty
								, !string.IsNullOrEmpty( hnrchar2 ) ? " " + hnrchar2 : string.Empty
								, x[0].ToString( decimalPlacesTxt, enUS )
								, y[0].ToString( decimalPlacesTxt, enUS )
							);
							tw.WriteLine( outline );
						}
						catch (Exception ex) {
							Console.WriteLine( "{1}======ERROR! line {0}:", lineCnter, Environment.NewLine );
							Console.WriteLine( line );
							Console.WriteLine( ex );
							Console.WriteLine( Environment.NewLine );
						}
					}
				}
			}


			//clean up
			Console.WriteLine( srcProjs["31254"].Definition );
            prjTarget.Dispose();
			prjTarget = null;
			srcProjs["31254"].Dispose();
			srcProjs["31254"] = null;
			srcProjs["31255"].Dispose();
			srcProjs["31255"] = null;
			srcProjs["31256"].Dispose();
			srcProjs["31256"] = null;

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
