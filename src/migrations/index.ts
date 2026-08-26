import * as migration_20260826_144329_release_schema from './20260826_144329_release_schema';
import * as migration_20260826_145044_media_provenance from './20260826_145044_media_provenance';

export const migrations = [
  {
    up: migration_20260826_144329_release_schema.up,
    down: migration_20260826_144329_release_schema.down,
    name: '20260826_144329_release_schema',
  },
  {
    up: migration_20260826_145044_media_provenance.up,
    down: migration_20260826_145044_media_provenance.down,
    name: '20260826_145044_media_provenance'
  },
];
