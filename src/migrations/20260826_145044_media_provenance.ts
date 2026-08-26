import { type MigrateDownArgs, type MigrateUpArgs, sql } from '@payloadcms/db-postgres'

export async function up({ db, payload, req }: MigrateUpArgs): Promise<void> {
  await db.execute(sql`
   ALTER TABLE "media" ADD COLUMN "legacy_attachment_id" numeric;
  ALTER TABLE "media" ADD COLUMN "legacy_path" varchar;
  CREATE INDEX "media_legacy_attachment_id_idx" ON "media" USING btree ("legacy_attachment_id");`)
}

export async function down({ db, payload, req }: MigrateDownArgs): Promise<void> {
  await db.execute(sql`
   DROP INDEX "media_legacy_attachment_id_idx";
  ALTER TABLE "media" DROP COLUMN "legacy_attachment_id";
  ALTER TABLE "media" DROP COLUMN "legacy_path";`)
}
