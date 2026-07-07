-- =============================================
-- Script: 002_CreateAdditionalTables.sql
-- Description: Creates trade_factor, interstate, interstate_factor,
--              trade_employment, trade_material, trade_resource,
--              trade_impact, and bea tables
-- Schema: https://model.earth/exiobase/tradeflow
-- Depends on: 001_CreateTradeTable.sql (industry, factor, trade)
-- =============================================

-- =============================================
-- 1. trade_factor
-- =============================================
CREATE TABLE public.trade_factor (
    trade_id    INTEGER     NOT NULL,
    year        SMALLINT    NOT NULL,
    country     VARCHAR(10) NOT NULL,
    flow_type   VARCHAR(10) NOT NULL,
    factor_id   INTEGER     NOT NULL REFERENCES public.factor(factor_id),
    coefficient NUMERIC,
    level       NUMERIC
);

CREATE INDEX idx_trade_factor_trade_id  ON public.trade_factor(trade_id);
CREATE INDEX idx_trade_factor_year      ON public.trade_factor(year);
CREATE INDEX idx_trade_factor_country   ON public.trade_factor(country);
CREATE INDEX idx_trade_factor_factor_id ON public.trade_factor(factor_id);

-- =============================================
-- 2. interstate
-- =============================================
CREATE TABLE public.interstate (
    id                   BIGSERIAL PRIMARY KEY,
    trade_id             INTEGER     NOT NULL,
    year                 SMALLINT    NOT NULL,
    region1              VARCHAR(10) NOT NULL,
    region2              VARCHAR(10) NOT NULL,
    industry1            VARCHAR(10) NOT NULL,
    industry2            VARCHAR(10) NOT NULL,
    amount               NUMERIC,
    commodity_code       VARCHAR(30),
    industry_code        VARCHAR(30),
    economic_multiplier  NUMERIC
);

CREATE INDEX idx_interstate_trade_id ON public.interstate(trade_id);
CREATE INDEX idx_interstate_year     ON public.interstate(year);
CREATE INDEX idx_interstate_regions  ON public.interstate(region1, region2);

-- =============================================
-- 3. interstate_factor
-- =============================================
CREATE TABLE public.interstate_factor (
    id                  BIGSERIAL PRIMARY KEY,
    interstate_id       VARCHAR(80),
    trade_id            INTEGER     NOT NULL,
    factor_id           INTEGER     NOT NULL REFERENCES public.factor(factor_id),
    state_industry_code VARCHAR(30),
    level               NUMERIC,
    flow_type           VARCHAR(20),
    employment_impact   NUMERIC
);

CREATE INDEX idx_interstate_factor_trade_id  ON public.interstate_factor(trade_id);
CREATE INDEX idx_interstate_factor_factor_id ON public.interstate_factor(factor_id);

-- =============================================
-- 4. trade_employment
-- =============================================
CREATE TABLE public.trade_employment (
    id               BIGSERIAL PRIMARY KEY,
    trade_id         INTEGER     NOT NULL,
    year             SMALLINT    NOT NULL,
    region1          VARCHAR(10) NOT NULL,
    region2          VARCHAR(10) NOT NULL,
    industry1        VARCHAR(10) NOT NULL,
    industry2        VARCHAR(10) NOT NULL,
    employment_value NUMERIC,
    flow_type        VARCHAR(10) NOT NULL,
    country          VARCHAR(10) NOT NULL
);

CREATE INDEX idx_trade_employment_year    ON public.trade_employment(year);
CREATE INDEX idx_trade_employment_country ON public.trade_employment(country);

-- =============================================
-- 5. trade_material
-- =============================================
CREATE TABLE public.trade_material (
    id             BIGSERIAL PRIMARY KEY,
    trade_id       INTEGER     NOT NULL,
    year           SMALLINT    NOT NULL,
    region1        VARCHAR(10) NOT NULL,
    region2        VARCHAR(10) NOT NULL,
    industry1      VARCHAR(10) NOT NULL,
    industry2      VARCHAR(10) NOT NULL,
    material_value NUMERIC,
    flow_type      VARCHAR(10) NOT NULL,
    country        VARCHAR(10) NOT NULL
);

CREATE INDEX idx_trade_material_year    ON public.trade_material(year);
CREATE INDEX idx_trade_material_country ON public.trade_material(country);

-- =============================================
-- 6. trade_resource
-- =============================================
CREATE TABLE public.trade_resource (
    id             BIGSERIAL PRIMARY KEY,
    trade_id       INTEGER     NOT NULL,
    year           SMALLINT    NOT NULL,
    region1        VARCHAR(10) NOT NULL,
    region2        VARCHAR(10) NOT NULL,
    industry1      VARCHAR(10) NOT NULL,
    industry2      VARCHAR(10) NOT NULL,
    resource_value NUMERIC,
    flow_type      VARCHAR(10) NOT NULL,
    country        VARCHAR(10) NOT NULL
);

CREATE INDEX idx_trade_resource_year    ON public.trade_resource(year);
CREATE INDEX idx_trade_resource_country ON public.trade_resource(country);

-- =============================================
-- 7. trade_impact
-- =============================================
CREATE TABLE public.trade_impact (
    id        BIGSERIAL PRIMARY KEY,
    trade_id  INTEGER     NOT NULL,
    year      SMALLINT    NOT NULL,
    region1   VARCHAR(10) NOT NULL,
    region2   VARCHAR(10) NOT NULL,
    industry1 VARCHAR(10) NOT NULL,
    industry2 VARCHAR(10) NOT NULL,
    level     NUMERIC,
    flow_type VARCHAR(10) NOT NULL,
    country   VARCHAR(10) NOT NULL
);

CREATE INDEX idx_trade_impact_year    ON public.trade_impact(year);
CREATE INDEX idx_trade_impact_country ON public.trade_impact(country);

-- =============================================
-- 8-10. BEA tables (structure matches interstate pattern)
-- =============================================
CREATE TABLE public.bea_table1 (
    id        BIGSERIAL PRIMARY KEY,
    trade_id  INTEGER     NOT NULL,
    year      SMALLINT    NOT NULL,
    region1   VARCHAR(10),
    region2   VARCHAR(10),
    industry1 VARCHAR(10),
    industry2 VARCHAR(10),
    bea_value NUMERIC,
    flow_type VARCHAR(10),
    country   VARCHAR(10)
);

CREATE TABLE public.bea_table2 (
    id        BIGSERIAL PRIMARY KEY,
    trade_id  INTEGER     NOT NULL,
    year      SMALLINT    NOT NULL,
    region1   VARCHAR(10),
    region2   VARCHAR(10),
    industry1 VARCHAR(10),
    industry2 VARCHAR(10),
    bea_value NUMERIC,
    flow_type VARCHAR(10),
    country   VARCHAR(10)
);

CREATE TABLE public.bea_table3 (
    id        BIGSERIAL PRIMARY KEY,
    trade_id  INTEGER     NOT NULL,
    year      SMALLINT    NOT NULL,
    region1   VARCHAR(10),
    region2   VARCHAR(10),
    industry1 VARCHAR(10),
    industry2 VARCHAR(10),
    bea_value NUMERIC,
    flow_type VARCHAR(10),
    country   VARCHAR(10)
);
