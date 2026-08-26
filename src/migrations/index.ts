import * as migration_20260826_144329_release_schema from './20260826_144329_release_schema';

export const migrations = [
  {
    up: migration_20260826_144329_release_schema.up,
    down: migration_20260826_144329_release_schema.down,
    name: '20260826_144329_release_schema'
  },
];
