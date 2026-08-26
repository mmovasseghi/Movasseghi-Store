import { type MigrateDownArgs, type MigrateUpArgs, sql } from '@payloadcms/db-postgres'

export async function up({ db }: MigrateUpArgs): Promise<void> {
  await db.execute(sql`
   CREATE TYPE "public"."enum_orders_payment_method" AS ENUM('phone', 'card_to_card', 'online');
  CREATE TYPE "public"."enum_orders_shipping_method" AS ENUM('seller', 'customer');
  CREATE TYPE "public"."enum_orders_status" AS ENUM('pending', 'confirmed', 'cancelled');
  CREATE TABLE "orders" (
  	"id" serial PRIMARY KEY NOT NULL,
  	"order_number" varchar NOT NULL,
  	"customer_name" varchar NOT NULL,
  	"customer_phone" varchar NOT NULL,
  	"note" varchar,
  	"payment_method" "enum_orders_payment_method" NOT NULL,
  	"shipping_method" "enum_orders_shipping_method" NOT NULL,
  	"items" jsonb NOT NULL,
  	"subtotal" numeric NOT NULL,
  	"status" "enum_orders_status" DEFAULT 'pending',
  	"updated_at" timestamp(3) with time zone DEFAULT now() NOT NULL,
  	"created_at" timestamp(3) with time zone DEFAULT now() NOT NULL
  );
  CREATE UNIQUE INDEX "orders_order_number_idx" ON "orders" USING btree ("order_number");
  ALTER TABLE "pages" ADD COLUMN "legacy_id" numeric;
  ALTER TABLE "pages" ADD COLUMN "legacy_content_html" varchar;`)
}

export async function down({ db }: MigrateDownArgs): Promise<void> {
  await db.execute(sql`
   ALTER TABLE "pages" DROP COLUMN IF EXISTS "legacy_content_html";
   ALTER TABLE "pages" DROP COLUMN IF EXISTS "legacy_id";
   DROP TABLE "orders" CASCADE;
   DROP TYPE "public"."enum_orders_status";
   DROP TYPE "public"."enum_orders_shipping_method";
   DROP TYPE "public"."enum_orders_payment_method";`)
}
