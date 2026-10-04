CREATE TABLE public.sites (
    site_id UUID PRIMARY KEY,
    site_name TEXT NOT NULL
);

CREATE TABLE public.cameras (
    camera_id UUID PRIMARY KEY,
    site_id UUID NOT NULL REFERENCES public.sites(site_id),
    camera_name TEXT NOT NULL
);

CREATE TABLE public.camera_events (
    event_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    camera_id UUID NOT NULL REFERENCES public.cameras(camera_id),
    site_id UUID NOT NULL REFERENCES public.sites(site_id),
    occurred_at_utc TIMESTAMPTZ NOT NULL,
    received_at_utc TIMESTAMPTZ NOT NULL,
    source_event_id TEXT NOT NULL,
    event_type TEXT NOT NULL,
    UNIQUE (camera_id, source_event_id)
);

REVOKE ALL ON TABLE public.sites, public.cameras, public.camera_events
FROM anon, authenticated;

REVOKE ALL ON SEQUENCE public.camera_events_event_id_seq
FROM anon, authenticated;
