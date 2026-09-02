CREATE DATABASE IF NOT EXISTS kitchen_analytics;

CREATE TABLE IF NOT EXISTS kitchen_analytics.order_timings
(
    timing_id     String,
    order_id      String,
    station       String,
    item_type     String,
    prep_seconds  Float64,
    recorded_at   DateTime
)
ENGINE = MergeTree()
ORDER BY (station, recorded_at);

CREATE TABLE IF NOT EXISTS kitchen_analytics.equipment_readings
(
    reading_id    String,
    equipment_id  String,
    metric        String,
    value         Float64,
    unit          String,
    recorded_at   DateTime
)
ENGINE = MergeTree()
ORDER BY (equipment_id, recorded_at);

CREATE TABLE IF NOT EXISTS kitchen_analytics.service_times
(
    service_id    String,
    order_id      String,
    item_type     String,
    wait_seconds  Float64,
    served_at     DateTime
)
ENGINE = MergeTree()
ORDER BY (item_type, served_at);
