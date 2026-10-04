CREATE TABLE "public"."camera_events" (
  "event_id"        bigint                   GENERATED ALWAYS AS IDENTITY NOT NULL,
  "camera_id"       uuid                     NOT NULL,
  "site_id"         uuid                     NOT NULL,
  "occurred_at_utc" timestamp with time zone NOT NULL,
  "received_at_utc" timestamp with time zone NOT NULL,
  "source_event_id" text                     NOT NULL,
  "event_type"      text                     NOT NULL,
  CONSTRAINT "camera_events_camera_id_source_event_id_key" UNIQUE (camera_id, source_event_id),
  CONSTRAINT "camera_events_pkey" PRIMARY KEY (event_id)
);

REVOKE ALL ON TABLE "public"."camera_events" FROM "anon", "authenticated";

CREATE TABLE "public"."cameras" (
  "camera_id"   uuid NOT NULL,
  "site_id"     uuid NOT NULL,
  "camera_name" text NOT NULL,
  CONSTRAINT "cameras_pkey" PRIMARY KEY (camera_id)
);

REVOKE ALL ON TABLE "public"."cameras" FROM "anon", "authenticated";

CREATE TABLE "public"."sites" (
  "site_id"   uuid NOT NULL,
  "site_name" text NOT NULL,
  CONSTRAINT "sites_pkey" PRIMARY KEY (site_id)
);

REVOKE ALL ON TABLE "public"."sites" FROM "anon", "authenticated";

REVOKE ALL ON SEQUENCE "public"."camera_events_event_id_seq" FROM "anon";

REVOKE ALL ON SEQUENCE "public"."camera_events_event_id_seq" FROM "authenticated";

ALTER TABLE "public"."camera_events"
  ADD CONSTRAINT "camera_events_camera_id_fkey" FOREIGN KEY (camera_id) REFERENCES public.cameras(camera_id);

ALTER TABLE "public"."camera_events"
  ADD CONSTRAINT "camera_events_site_id_fkey" FOREIGN KEY (site_id) REFERENCES public.sites(site_id);

ALTER TABLE "public"."cameras"
  ADD CONSTRAINT "cameras_site_id_fkey" FOREIGN KEY (site_id) REFERENCES public.sites(site_id);

REVOKE ALL ON SEQUENCE "public"."camera_events_event_id_seq" FROM "postgres";

GRANT SELECT, UPDATE, USAGE ON SEQUENCE "public"."camera_events_event_id_seq" TO "postgres";

REVOKE ALL ON SEQUENCE "public"."camera_events_event_id_seq" FROM "service_role";

GRANT SELECT, UPDATE, USAGE ON SEQUENCE "public"."camera_events_event_id_seq" TO "service_role";

REVOKE ALL ON TABLE "public"."camera_events" FROM "postgres";

GRANT DELETE, INSERT, MAINTAIN, REFERENCES, SELECT, TRIGGER, TRUNCATE, UPDATE ON TABLE "public"."camera_events" TO "postgres";

GRANT DELETE, INSERT, MAINTAIN, REFERENCES, SELECT, TRIGGER, TRUNCATE, UPDATE ON TABLE "public"."camera_events" TO "service_role";

REVOKE ALL ON TABLE "public"."cameras" FROM "postgres";

GRANT DELETE, INSERT, MAINTAIN, REFERENCES, SELECT, TRIGGER, TRUNCATE, UPDATE ON TABLE "public"."cameras" TO "postgres";

GRANT DELETE, INSERT, MAINTAIN, REFERENCES, SELECT, TRIGGER, TRUNCATE, UPDATE ON TABLE "public"."cameras" TO "service_role";

REVOKE ALL ON TABLE "public"."sites" FROM "postgres";

GRANT DELETE, INSERT, MAINTAIN, REFERENCES, SELECT, TRIGGER, TRUNCATE, UPDATE ON TABLE "public"."sites" TO "postgres";

GRANT DELETE, INSERT, MAINTAIN, REFERENCES, SELECT, TRIGGER, TRUNCATE, UPDATE ON TABLE "public"."sites" TO "service_role";
