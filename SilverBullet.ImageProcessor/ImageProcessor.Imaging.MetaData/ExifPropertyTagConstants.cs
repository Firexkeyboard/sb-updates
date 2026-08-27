using System;
using System.Linq;

namespace ImageProcessor.Imaging.MetaData;

public static class ExifPropertyTagConstants
{
	public static readonly ExifPropertyTag[] RequiredPropertyItems = new ExifPropertyTag[2]
	{
		ExifPropertyTag.LoopCount,
		ExifPropertyTag.FrameDelay
	};

	public static readonly ExifPropertyTag[] GeolocationPropertyItems = RequiredPropertyItems.Union(new ExifPropertyTag[28]
	{
		ExifPropertyTag.GpsAltitude,
		ExifPropertyTag.GpsAltitudeRef,
		ExifPropertyTag.GpsDestBear,
		ExifPropertyTag.GpsDestBearRef,
		ExifPropertyTag.GpsDestDist,
		ExifPropertyTag.GpsDestDistRef,
		ExifPropertyTag.GpsDestLat,
		ExifPropertyTag.GpsDestLatRef,
		ExifPropertyTag.GpsDestLong,
		ExifPropertyTag.GpsDestLongRef,
		ExifPropertyTag.GpsGpsDop,
		ExifPropertyTag.GpsGpsMeasureMode,
		ExifPropertyTag.GpsGpsSatellites,
		ExifPropertyTag.GpsGpsStatus,
		ExifPropertyTag.GpsGpsTime,
		ExifPropertyTag.GpsIFD,
		ExifPropertyTag.GpsImgDir,
		ExifPropertyTag.GpsImgDirRef,
		ExifPropertyTag.GpsLatitude,
		ExifPropertyTag.GpsLatitudeRef,
		ExifPropertyTag.GpsLongitude,
		ExifPropertyTag.GpsLongitudeRef,
		ExifPropertyTag.GpsMapDatum,
		ExifPropertyTag.GpsSpeed,
		ExifPropertyTag.GpsSpeedRef,
		ExifPropertyTag.GpsTrack,
		ExifPropertyTag.GpsTrackRef,
		ExifPropertyTag.GpsVer
	}).ToArray();

	public static readonly ExifPropertyTag[] CopyrightPropertyItems = RequiredPropertyItems.Union(new ExifPropertyTag[12]
	{
		ExifPropertyTag.Copyright,
		ExifPropertyTag.Artist,
		ExifPropertyTag.ImageTitle,
		ExifPropertyTag.ImageDescription,
		ExifPropertyTag.ExifUserComment,
		ExifPropertyTag.EquipMake,
		ExifPropertyTag.EquipModel,
		ExifPropertyTag.ThumbnailArtist,
		ExifPropertyTag.ThumbnailCopyRight,
		ExifPropertyTag.ThumbnailImageDescription,
		ExifPropertyTag.ThumbnailEquipMake,
		ExifPropertyTag.ThumbnailEquipModel
	}).ToArray();

	public static readonly ExifPropertyTag[] CopyrightAndGeolocationPropertyItems = GeolocationPropertyItems.Union(CopyrightPropertyItems).ToArray();

	public static readonly ExifPropertyTag[] All = (ExifPropertyTag[])Enum.GetValues(typeof(ExifPropertyTag));
}
