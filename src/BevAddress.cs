using System;
using FileHelpers;

[IgnoreFirst( 1 )]
[DelimitedRecord( ";" )]
public sealed class BevAddress {

	[FieldTrim( TrimMode.Both )]
	[FieldQuoted( '"', QuoteMode.OptionalForRead, MultilineMode.AllowForRead )]
	public String ADRCD;

	[FieldTrim( TrimMode.Both )]
	[FieldQuoted( '"', QuoteMode.OptionalForRead, MultilineMode.AllowForRead )]
	public String GKZ;

	[FieldTrim( TrimMode.Both )]
	[FieldQuoted( '"', QuoteMode.OptionalForRead, MultilineMode.AllowForRead )]
	public String OKZ;

	[FieldTrim( TrimMode.Both )]
	[FieldQuoted( '"', QuoteMode.OptionalForRead, MultilineMode.AllowForRead )]
	public String PLZ;

	[FieldTrim( TrimMode.Both )]
	[FieldQuoted( '"', QuoteMode.OptionalForRead, MultilineMode.AllowForRead )]
	public String SKZ;

	[FieldTrim( TrimMode.Both )]
	[FieldQuoted( '"', QuoteMode.OptionalForRead, MultilineMode.AllowForRead )]
	public String ZAEHLSPRENGEL;

	[FieldTrim( TrimMode.Both )]
	[FieldQuoted( '"', QuoteMode.OptionalForRead, MultilineMode.AllowForRead )]
	public String HAUSNRTEXT;

	[FieldTrim( TrimMode.Both )]
	public Int32? HAUSNRZAHL1;

	[FieldTrim( TrimMode.Both )]
	[FieldQuoted( '"', QuoteMode.OptionalForRead, MultilineMode.AllowForRead )]
	public String HAUSNRBUCHSTABE1;

	[FieldTrim( TrimMode.Both )]
	[FieldQuoted( '"', QuoteMode.OptionalForRead, MultilineMode.AllowForRead )]
	public String HAUSNRVERBINDUNG1;

	[FieldTrim( TrimMode.Both )]
	public Int32? HAUSNRZAHL2;

	[FieldTrim( TrimMode.Both )]
	[FieldQuoted( '"', QuoteMode.OptionalForRead, MultilineMode.AllowForRead )]
	public String HAUSNRBUCHSTABE2;

	[FieldTrim( TrimMode.Both )]
	[FieldQuoted( '"', QuoteMode.OptionalForRead, MultilineMode.AllowForRead )]
	public String HAUSNRBEREICH;

	[FieldTrim( TrimMode.Both )]
	public Int32 GNRADRESSE;

	[FieldTrim( TrimMode.Both )]
	[FieldQuoted( '"', QuoteMode.OptionalForRead, MultilineMode.AllowForRead )]
	public String HOFNAME;

	[FieldTrim( TrimMode.Both )]
	public Double? RW;

	[FieldTrim( TrimMode.Both )]
	public Double? HW;

	[FieldTrim( TrimMode.Both )]
	public String EPSG;

	[FieldTrim( TrimMode.Both )]
	[FieldQuoted( '"', QuoteMode.OptionalForRead, MultilineMode.AllowForRead )]
	public String QUELLADRESSE;

	[FieldTrim( TrimMode.Both )]
	[FieldQuoted( '"', QuoteMode.OptionalForRead, MultilineMode.AllowForRead )]
	public String BESTIMMUNGSART;


}