CREATE INDEX camera_events_site_occurred_idx
ON public.camera_events (site_id, occurred_at_utc);
