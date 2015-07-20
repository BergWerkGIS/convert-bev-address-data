using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ProjApi;

namespace convert_bev_address_data {


	class Program {

		static void Main( string[] args ) {

			if (args.Length < 1) {
				Console.WriteLine( "usage:" );
				Console.WriteLine( "      convert-bev-address-data <FULL-PATH-TO-ADRESSE.csv>" );
				Console.WriteLine( "      also needs STRASSE.csv in same directory" );
				return;
			}

			string adrCsv = args[0];
			if (!File.Exists( adrCsv )) {
				Console.WriteLine( "not found: {0}", adrCsv );
				return;
			}

			string dataDir = Path.GetDirectoryName( adrCsv );
			string strCsv = Path.Combine( dataDir, "STRASSE.csv" );
			if (!File.Exists( strCsv )) {
				Console.WriteLine( "not found: {0}", strCsv );
				return;
			}

			Console.WriteLine( "reading street names" );
			Dictionary<string, string> streets = new Dictionary<string, string>();

			char[] delimiter = ";".ToCharArray();
			CultureInfo enUS = new CultureInfo( "en-US" );

			using (TextReader tr = new StreamReader( strCsv )) {

				//skip header
				tr.ReadLine();

				string line;

				while (!string.IsNullOrWhiteSpace( line = tr.ReadLine() )) {

					string[] tokens = line.Split( delimiter );
					string skz = tokens[0].Replace( "\"", string.Empty );
					string name = tokens[1].Replace( "\"", string.Empty );

					streets.Add( skz, name );
				}
			}

			string csvOut = Path.Combine( dataDir, "out.csv" );

			Dictionary<string, Projection> srcProjs = new Dictionary<string, Projection>();
			srcProjs.Add( "31254", new Projection( "+init=epsg:31254" ) );
			srcProjs.Add( "31255", new Projection( "+init=epsg:31255" ) );
			srcProjs.Add( "31256", new Projection( "+init=epsg:31256" ) );

			Projection epsg31287 = new Projection( "+init=epsg:31287" );

			Console.WriteLine( "reading addresses" );

			using (TextReader tr = new StreamReader( adrCsv )) {

				//skip header
				tr.ReadLine();

				string line;
				Int64 lineCnter = 1;

				using (TextWriter tw = new StreamWriter( csvOut, false, Encoding.UTF8 )) {

					tw.WriteLine( "address;x-EPSG-31287;y-EPSG-31287" );
					while (!string.IsNullOrWhiteSpace( line = tr.ReadLine() )) {

						lineCnter++;
						try {
							string[] tokens = line.Split( delimiter );

							string skz = tokens[4].Replace( "\"", string.Empty );
							string nrtxt = tokens[6].Replace( "\"", string.Empty );
							string nrnr = tokens[7].Replace( "\"", string.Empty );
							string nrchar = tokens[8].Replace( "\"", string.Empty );
							string epsg = tokens[17].Replace( "\"", string.Empty );
							string xTxt = tokens[15].Replace( "\"", string.Empty );
							string yTxt = tokens[16].Replace( "\"", string.Empty );

							double[] x;
							double[] y;

							if (string.IsNullOrWhiteSpace( xTxt ) || string.IsNullOrWhiteSpace( yTxt )) {
								x = new double[] { -1 };
								y = new double[] { -1 };
							} else {
								x = new double[] { Convert.ToDouble( xTxt, enUS ) };
								y = new double[] { Convert.ToDouble( yTxt, enUS ) };

								srcProjs[epsg].Transform( epsg31287, x, y );
							}
							string outline = string.Format(
								enUS
								, "{0} {1} {2} {3};{4:0.#};{5:0.#}"
								, streets.ContainsKey( skz ) ? streets[skz] : string.Empty
								, nrtxt
								, nrnr
								, nrchar
								, x[0]
								, y[0]
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
			epsg31287.Dispose();
			epsg31287 = null;
			srcProjs["31254"].Dispose();
			srcProjs["31254"] = null;
			srcProjs["31255"].Dispose();
			srcProjs["31255"] = null;
			srcProjs["31256"].Dispose();
			srcProjs["31256"] = null;

		}




	}
}
