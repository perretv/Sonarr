using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(230)]
    public class block_downloads_before_airdate : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("INSERT INTO \"Config\" (\"Key\", \"Value\") VALUES ('BlockDownloadsBeforeAirdate', 'False');");
        }
    }
}
