using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ProjApi;
using Mono.Options;
using FileHelpers;
using System.Diagnostics;

namespace convert_bev_address_data {


    class Program {

        static int Main(string[] args) {

            string binDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            Proj.pj_set_searchpath(1, new string[] { binDir });

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
                extra = p.Parse(args);
            } catch (OptionException e) {
                Console.WriteLine(e.Message);
                Console.WriteLine("try 'convert-bev-address-data --usage'");
                return 1;
            }

            if (args.Length < 1 || string.IsNullOrWhiteSpace(inputDir) || _ShowUsage) {
                ShowUsage(p);
                return 1;
            }

            string csvAddress = Path.Combine(inputDir, "ADRESSE.CSV");
            string csvStreet = Path.Combine(inputDir, "STRASSE.CSV");
            string csvGemeinde = Path.Combine(inputDir, "GEMEINDE.CSV");
            string csvOrt = Path.Combine(inputDir, "ORTSCHAFT.CSV");
            string csvGstk = Path.Combine(inputDir, "ADRESSE_GST.CSV");

            if (!filesExist(csvAddress, csvStreet, csvGemeinde, csvOrt, csvGstk)) {
                return 1;
            }

            if (string.IsNullOrWhiteSpace(csvOut)) {
                csvOut = Path.Combine(inputDir, "out.csv");
            } else {
                string outPath = Path.GetFullPath(csvOut);
                string outDir = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrWhiteSpace(outPath) && !Directory.Exists(outDir)) {
                    Console.WriteLine("output directory does not exists: {0}", outDir);
                    return 1;
                }
            }


            Dictionary<string, Projection> targetProjs = new Dictionary<string, Projection>();
            targetProjs.Add("31254", new Projection("+proj=tmerc +lat_0=0 +lon_0=10.33333333333333 +k=1 +x_0=0 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs"));
            targetProjs.Add("31255", new Projection("+proj=tmerc +lat_0=0 +lon_0=13.33333333333333 +k=1 +x_0=0 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs"));
            targetProjs.Add("31256", new Projection("+proj=tmerc +lat_0=0 +lon_0=16.33333333333333 +k=1 +x_0=0 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs "));
            targetProjs.Add("31287", new Projection("+proj=lcc +lat_1=49 +lat_2=46 +lat_0=47.5 +lon_0=13.33333333333333 +x_0=400000 +y_0=400000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs"));

            Projection prjTarget = null;
            try {
                if (targetProjs.ContainsKey(targetEpsg)) {
                    prjTarget = targetProjs[targetEpsg];
                } else {
                    prjTarget = new Projection("+init=epsg:" + targetEpsg);
                }
            } catch (Exception ex) {
                Console.WriteLine("could not initialize EPSG:" + targetEpsg);
                Console.WriteLine(ex.Message);
                return 1;
            }
            if (null == targetEpsg) {
                Console.WriteLine("could not initialize EPSG:" + targetEpsg);
                return 1;
            }
            Console.WriteLine("output crs, EPSG:" + targetEpsg);

            char[] delimiter = ";".ToCharArray();
            CultureInfo enUS = new CultureInfo("en-US");
            enUS.NumberFormat.NumberGroupSeparator = string.Empty;
            string decimalPlacesTxt = string.Format("N{0}", decimalPlaces);


            FileStreamOptions streamOptions = new() {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Options = FileOptions.None,
                Share = FileShare.Read
            };

            Console.WriteLine("reading street names");
            Dictionary<string, string> streets = new Dictionary<string, string>();
            using (TextReader tr = new StreamReader(csvStreet, streamOptions)) {
                tr.ReadLine();
                string line;
                while (!string.IsNullOrWhiteSpace(line = tr.ReadLine())) {
                    string[] tokens = line.Split(delimiter);
                    string skz = tokens[0].Replace("\"", string.Empty);
                    string name = tokens[1].Replace("\"", string.Empty);
                    streets.Add(skz, name);
                }
            }


            Console.WriteLine("reading orte");
            Dictionary<string, string> orte = new Dictionary<string, string>();
            using (TextReader tr = new StreamReader(csvOrt, streamOptions)) {
                tr.ReadLine();
                string line;
                while (!string.IsNullOrWhiteSpace(line = tr.ReadLine())) {
                    string[] tokens = line.Split(delimiter);
                    string okz = tokens[1].Replace("\"", string.Empty);
                    string name = tokens[2].Replace("\"", string.Empty);
                    orte.Add(okz, name);
                }
            }


            Console.WriteLine("reading gemeinden");
            Dictionary<string, string> gemeinden = new Dictionary<string, string>();
            using (TextReader tr = new StreamReader(csvGemeinde, streamOptions)) {
                tr.ReadLine();
                string line;
                while (!string.IsNullOrWhiteSpace(line = tr.ReadLine())) {
                    string[] tokens = line.Split(delimiter);
                    string gkz = tokens[0].Replace("\"", string.Empty);
                    string name = tokens[1].Replace("\"", string.Empty);
                    gemeinden.Add(gkz, name);
                }
            }



            Console.WriteLine("reading gstk");
            // ADRCD, KGNR, GSTKNR
            Dictionary<string, (string KGNR, string GSTNR)> gstk = new Dictionary<string, (string, string)>();
            long duplicateCount = 0;
            using (TextReader tr = new StreamReader(csvGstk, streamOptions)) {
                tr.ReadLine();
                string line;
                while (!string.IsNullOrWhiteSpace(line = tr.ReadLine())) {
                    string[] tokens = line.Split(delimiter);
                    string adrcd = tokens[0].Replace("\"", string.Empty);
                    string kgnr = tokens[1].Replace("\"", string.Empty);
                    string gstnr = tokens[2].Replace("\"", string.Empty);
                    if (gstk.ContainsKey(adrcd)) {
                        duplicateCount++;
                        //var g = gstk[adrcd];
                        //string msg = $"ARDCD[{adrcd}] already added: [{g.KGNR}] [{g.GSTNR}] => [{kgnr}] [{gstnr}]";
                        //Console.WriteLine(msg);
                        //System.Diagnostics.Debug.WriteLine(msg);
                    } else {
                        gstk.Add(adrcd, (kgnr, gstnr));
                    }
                }
            }
            if (duplicateCount > 0) {
                Console.WriteLine($"{duplicateCount} addresses mapped to several gst");
            }


            Dictionary<string, Projection> srcProjs = new Dictionary<string, Projection>();
            //wrong towgs84 paramters
            //srcProjs.Add( "31254", new Projection( "+init=epsg:31254" ) );
            //srcProjs.Add( "31254", new Projection( "+init=epsg:31254" ) );
            //srcProjs.Add( "31254", new Projection( "+init=epsg:31254" ) );
            srcProjs.Add("31254", new Projection("+proj=tmerc +lat_0=0 +lon_0=10.33333333333333 +k=1 +x_0=0 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs"));
            srcProjs.Add("31255", new Projection("+proj=tmerc +lat_0=0 +lon_0=13.33333333333333 +k=1 +x_0=0 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs"));
            srcProjs.Add("31256", new Projection("+proj=tmerc +lat_0=0 +lon_0=16.33333333333333 +k=1 +x_0=0 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs "));


            Console.WriteLine("reading and writing addresses");

            var fileHelper = new FileHelperAsyncEngine<BevAddress>();
            long skipCnt = 0;

            using (fileHelper.BeginReadFile(csvAddress)) {

                using (TextWriter tw = new StreamWriter(csvOut, false, Encoding.UTF8)) {

                    tw.WriteLine("adrcd;gkz;gemeinde;kgnr;gstk;plz;ort;strasse;hnr;x-{0};y-{0}", targetEpsg);
                    long lineCnter = 1; //start with 1 (skipped header)
                    foreach (var record in fileHelper) {

                        lineCnter++;

                        if (!record.RW.HasValue || !record.HW.HasValue) {
                            skipCnt++;
                            Console.WriteLine("no coordinates: {0}", recordToString(record, streets));
                            continue;
                        }

                        double[] x = new double[] { record.RW.Value };
                        double[] y = new double[] { record.HW.Value };

                        try {
                            srcProjs[record.EPSG].Transform(prjTarget, x, y);
                        } catch (Exception ex2) {
                            Console.WriteLine("{1}======ERROR: prj transform failed! line {0}:", lineCnter, Environment.NewLine);
                            Console.WriteLine(recordToString(record, streets));
                            Console.WriteLine(ex2);
                            Console.WriteLine(Environment.NewLine);
                            continue;
                        }

                        string outline = string.Format(
                            enUS
                            , "{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10}"
                            , record.ADRCD
                            , record.GKZ
                            , gemeinden.ContainsKey(record.GKZ) ? gemeinden[record.GKZ] : string.Empty
                            , gstk.ContainsKey(record.ADRCD) ? gstk[record.ADRCD].KGNR : string.Empty
                            , gstk.ContainsKey(record.ADRCD) ? gstk[record.ADRCD].GSTNR : string.Empty
                            , record.PLZ
                            , orte.ContainsKey(record.OKZ) ? orte[record.OKZ] : string.Empty
                            , streets.ContainsKey(record.SKZ) ? streets[record.SKZ] : string.Empty
                            , constructHouseNumber(record)
                            , x[0].ToString(decimalPlacesTxt, enUS)
                            , y[0].ToString(decimalPlacesTxt, enUS)
                        );

                        tw.WriteLine(outline);
                    }
                }
            }

            //clean up
            Console.WriteLine("skipped: {0}{1}{0}", Environment.NewLine, skipCnt);


            /// DON'T DO THIS, IT WILL CRASH THE APP
            /// as we dispose all targetProjs later on and 
            /// prjTarget is a reference to one of them
            //try {
            //    prjTarget.Dispose();
            //    prjTarget = null;
            //} catch (Exception ex) {
            //    Console.WriteLine("prjTarget.Dispose() failed");
            //    Console.WriteLine(ex.Message);
            //}

            foreach (var x in srcProjs) {
                try {
                    Console.WriteLine($"disposing srcProj[{x.Key}]");
                    x.Value.Dispose();
                } catch (Exception ex) {
                    Console.WriteLine($"srcProjs[{x.Key}].Dispose() failed");
                    Console.WriteLine(ex.Message);
                }
            }

            foreach (var x in targetProjs) {
                try {
                    Console.WriteLine($"disposing targetProj[{x.Key}]");
                    x.Value.Dispose();
                } catch (Exception ex) {
                    Console.WriteLine($"targetProjs[{x.Key}].Dispose() failed");
                    Console.WriteLine(ex.Message);
                }
            }

            Console.WriteLine($"{Environment.NewLine}done");
            return 0;
        }


        private static string constructHouseNumber(BevAddress record) {

            List<string> hnr = new List<string>();
            if (!string.IsNullOrEmpty(record.HAUSNRTEXT)) { hnr.Add(record.HAUSNRTEXT); }
            if (record.HAUSNRZAHL1.HasValue) { hnr.Add(record.HAUSNRZAHL1.ToString()); }
            if (!string.IsNullOrEmpty(record.HAUSNRBUCHSTABE1)) { hnr.Add(record.HAUSNRBUCHSTABE1); }
            if (!string.IsNullOrEmpty(record.HAUSNRVERBINDUNG1)) { hnr.Add(record.HAUSNRVERBINDUNG1); }
            if (record.HAUSNRZAHL2.HasValue) { hnr.Add(record.HAUSNRZAHL2.ToString()); }
            if (!string.IsNullOrEmpty(record.HAUSNRBUCHSTABE2)) { hnr.Add(record.HAUSNRBUCHSTABE2); }

            return string.Join(" ", hnr.ToArray());
        }


        private static string recordToString(BevAddress record, Dictionary<string, string> streets) {
            return string.Format(
                "adrcd:{0} gkz:{1} okz:{2} plz:{3} {4}"
                , record.ADRCD
                , record.GKZ
                , record.OKZ
                , record.PLZ
                , streets.ContainsKey(record.SKZ) ? streets[record.SKZ] : string.Empty
            );
        }


        private static void ShowUsage(OptionSet p) {
            Console.WriteLine("usage:");
            Console.WriteLine("      convert-bev-address-data -i <PATH-TO-BEV-DATA-DIRECTORY> [-o] [-e] [-d]");
            Console.WriteLine();
            Console.WriteLine("options:");
            p.WriteOptionDescriptions(Console.Out);
        }


        private static bool filesExist(params string[] files) {

            bool retVal = true;

            foreach (var file in files) {
                if (!File.Exists(file)) {
                    Console.WriteLine("not found: {0}", file);
                    retVal = false;
                }
            }

            return retVal;
        }



    }
}
