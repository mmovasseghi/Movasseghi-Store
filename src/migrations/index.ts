import * as migration_20260826_144329_release_schema from './20260826_144329_release_schema'
import * as migration_20260826_145044_media_provenance from './20260826_145044_media_provenance'
import * as migration_20260826_150606_product_legacy_html from './20260826_150606_product_legacy_html'
import * as migration_20260826_160000_orders_pages from './20260826_160000_orders_pages'
import * as migration_20260826_160100_pages_html_text from './20260826_160100_pages_html_text'
import * as migration_20260826_160200_orders_rels from './20260826_160200_orders_rels'
import * as migration_20260826_170000_blog_posts from './20260826_170000_blog_posts'

export const migrations = [
  {
    up: migration_20260826_144329_release_schema.up,
    down: migration_20260826_144329_release_schema.down,
    name: '20260826_144329_release_schema',
  },
  {
    up: migration_20260826_145044_media_provenance.up,
    down: migration_20260826_145044_media_provenance.down,
    name: '20260826_145044_media_provenance',
  },
  {
    up: migration_20260826_150606_product_legacy_html.up,
    down: migration_20260826_150606_product_legacy_html.down,
    name: '20260826_150606_product_legacy_html',
  },
  {
    up: migration_20260826_160000_orders_pages.up,
    down: migration_20260826_160000_orders_pages.down,
    name: '20260826_160000_orders_pages',
  },
  {
    up: migration_20260826_160100_pages_html_text.up,
    down: migration_20260826_160100_pages_html_text.down,
    name: '20260826_160100_pages_html_text',
  },
  {
    up: migration_20260826_160200_orders_rels.up,
    down: migration_20260826_160200_orders_rels.down,
    name: '20260826_160200_orders_rels',
  },
  {
    up: migration_20260826_170000_blog_posts.up,
    down: migration_20260826_170000_blog_posts.down,
    name: '20260826_170000_blog_posts',
  },
]
