CREATE TABLE IF NOT EXISTS pdm_object
(
    id BIGSERIAL PRIMARY KEY,

    object_type VARCHAR(32) NOT NULL,

    designation VARCHAR(32) NULL,

    name VARCHAR(255) NOT NULL,

    current_version_id BIGINT NULL,

    CONSTRAINT ck_pdm_object_type
        CHECK (object_type IN ('Assembly', 'Part', 'StandardPart')),

    CONSTRAINT ck_pdm_object_identity
        CHECK (
            (
                object_type IN ('Assembly', 'Part')
                AND designation IS NOT NULL
            )
            OR
            (
                object_type = 'StandardPart'
                AND designation IS NULL
            )
        )
);


CREATE UNIQUE INDEX IF NOT EXISTS ux_pdm_object_designation
    ON pdm_object (designation)
    WHERE designation IS NOT NULL;


CREATE UNIQUE INDEX IF NOT EXISTS ux_pdm_object_standard_part_name
    ON pdm_object (name)
    WHERE object_type = 'StandardPart';


CREATE TABLE IF NOT EXISTS object_version
(
    id BIGSERIAL PRIMARY KEY,

    object_id BIGINT NOT NULL,

    version_no INTEGER NOT NULL,

    state VARCHAR(32) NOT NULL,

    material VARCHAR(255) NULL,

    mass_kg NUMERIC(18, 6) NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_object_version_object
        FOREIGN KEY (object_id)
        REFERENCES pdm_object (id)
        ON DELETE CASCADE,

    CONSTRAINT uq_object_version_number
        UNIQUE (object_id, version_no),

    CONSTRAINT ck_object_version_number
        CHECK (version_no > 0),

    CONSTRAINT ck_object_version_state
        CHECK (state IN ('InWork', 'Approved', 'Cancelled')),

    CONSTRAINT ck_object_version_mass
        CHECK (mass_kg IS NULL OR mass_kg >= 0)
);


ALTER TABLE pdm_object
    DROP CONSTRAINT IF EXISTS fk_pdm_object_current_version;

ALTER TABLE pdm_object
    ADD CONSTRAINT fk_pdm_object_current_version
        FOREIGN KEY (current_version_id)
        REFERENCES object_version (id)
        ON DELETE SET NULL;


CREATE TABLE IF NOT EXISTS bom_link
(
    id BIGSERIAL PRIMARY KEY,

    parent_version_id BIGINT NOT NULL,

    child_object_id BIGINT NOT NULL,

    quantity INTEGER NOT NULL,

    CONSTRAINT fk_bom_link_parent_version
        FOREIGN KEY (parent_version_id)
        REFERENCES object_version (id)
        ON DELETE CASCADE,

    CONSTRAINT fk_bom_link_child_object
        FOREIGN KEY (child_object_id)
        REFERENCES pdm_object (id)
        ON DELETE RESTRICT,

    CONSTRAINT uq_bom_link_parent_child
        UNIQUE (parent_version_id, child_object_id),

    CONSTRAINT ck_bom_link_quantity
        CHECK (quantity > 0)
);