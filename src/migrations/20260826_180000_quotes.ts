import { type MigrateDownArgs, type MigrateUpArgs, sql } from '@payloadcms/db-postgres'

export async function up({ db }: MigrateUpArgs): Promise<void> {
  await db.execute(sql`
    CREATE TYPE "public"."enum_quotes_shipping_preference" AS ENUM('seller', 'customer', 'unknown');
    CREATE TYPE "public"."enum_quotes_status" AS ENUM('new', 'in_progress', 'answered', 'closed');
    CREATE TABLE "quotes" (
      "id" serial PRIMARY KEY NOT NULL,
      "quote_number" varchar NOT NULL,
      "company_name" varchar NOT NULL,
      "contact_name" varchar NOT NULL,
      "contact_phone" varchar NOT NULL,
      "city" varchar,
      "products_note" varchar NOT NULL,
      "shipping_preference" "enum_quotes_shipping_preference" DEFAULT 'unknown',
      "note" varchar,
      "status" "enum_quotes_status" DEFAULT 'new',
      "updated_at" timestamp(3) with time zone DEFAULT now() NOT NULL,
      "created_at" timestamp(3) with time zone DEFAULT now() NOT NULL
    );
    CREATE UNIQUE INDEX "quotes_quote_number_idx" ON "quotes" USING btree ("quote_number");
  `)
}

export async function down({ db }: MigrateDownArgs): Promise<void> {
  await db.execute(sql`
    DROP TABLE IF EXISTS "quotes" CASCADE;
    DROP TYPE IF EXISTS "public"."enum_quotes_status";
    DROP TYPE IF EXISTS "public"."enum_quotes_shipping_preference";
  `)
}
