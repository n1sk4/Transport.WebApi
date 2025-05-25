using System.Runtime.Serialization;

namespace Transport.WebApi.Options;

public enum GtfsStaticDataFile
{
  [EnumMember(Value = "Agency")]
  AgencyFile,
  [EnumMember(Value = "Stops")]
  StopsFile,
  [EnumMember(Value = "Routes")]
  RoutesFile,
  [EnumMember(Value = "Trips")]
  TripsFile,
  [EnumMember(Value = "Stop Times")]
  StopTimesFile,
  [EnumMember(Value = "Calendar")]
  CalendarFile,
  [EnumMember(Value = "Calendar Dates")]
  CalendarDatesFile,
  [EnumMember(Value = "Fare Attributes")]
  FareAttributesFile,
  [EnumMember(Value = "Fare Rules")]
  FareRulesFile,
  [EnumMember(Value = "Shapes")]
  ShapesFile,
  [EnumMember(Value = "Frequencies")]
  FrequenciesFile,
  [EnumMember(Value = "Transfers")]
  TransfersFile,
  [EnumMember(Value = "Pathways")]
  PathwaysFile,
  [EnumMember(Value = "Levels")]
  LevelsFile,
  [EnumMember(Value = "Feed Info")]
  FeedInfoFile
}

public static class GtfsStaticDataFileExtensions
{
  public static string GetFileName(this GtfsStaticDataFile file)
  {
    return file switch
    {
      GtfsStaticDataFile.AgencyFile => "agency.txt",
      GtfsStaticDataFile.StopsFile => "stops.txt",
      GtfsStaticDataFile.RoutesFile => "routes.txt",
      GtfsStaticDataFile.TripsFile => "trips.txt",
      GtfsStaticDataFile.StopTimesFile => "stop_times.txt",
      GtfsStaticDataFile.CalendarFile => "calendar.txt",
      GtfsStaticDataFile.CalendarDatesFile => "calendar_dates.txt",
      GtfsStaticDataFile.FareAttributesFile => "fare_attributes.txt",
      GtfsStaticDataFile.FareRulesFile => "fare_rules.txt",
      GtfsStaticDataFile.ShapesFile => "shapes.txt",
      GtfsStaticDataFile.FrequenciesFile => "frequencies.txt",
      GtfsStaticDataFile.TransfersFile => "transfers.txt",
      GtfsStaticDataFile.PathwaysFile => "pathways.txt",
      GtfsStaticDataFile.LevelsFile => "levels.txt",
      GtfsStaticDataFile.FeedInfoFile => "feed_info.txt",
      _ => throw new ArgumentOutOfRangeException(nameof(file), file, null)
    };
  }
}
