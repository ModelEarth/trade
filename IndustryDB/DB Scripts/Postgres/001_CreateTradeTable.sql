-- =============================================
-- Script: 001_CreateTradeTable.sql
-- Description: Creates primary tables: industry, factor, trade
-- Schema: https://model.earth/exiobase/tradeflow
-- =============================================

-- Drop tables in reverse dependency order
DROP TABLE IF EXISTS public.interstate_factor CASCADE;
DROP TABLE IF EXISTS public.interstate CASCADE;
DROP TABLE IF EXISTS public.trade_factor CASCADE;
DROP TABLE IF EXISTS public.trade_employment CASCADE;
DROP TABLE IF EXISTS public.trade_material CASCADE;
DROP TABLE IF EXISTS public.trade_resource CASCADE;
DROP TABLE IF EXISTS public.trade_impact CASCADE;
DROP TABLE IF EXISTS public.bea_table1 CASCADE;
DROP TABLE IF EXISTS public.bea_table2 CASCADE;
DROP TABLE IF EXISTS public.bea_table3 CASCADE;
DROP TABLE IF EXISTS public.trade CASCADE;
DROP TABLE IF EXISTS public.factor CASCADE;
DROP TABLE IF EXISTS public.industry CASCADE;

-- =============================================
-- 1. industry
-- =============================================
CREATE TABLE public.industry (
    industry_id VARCHAR(10) PRIMARY KEY,
    name        TEXT,
    category    VARCHAR(100)
);

-- =============================================
-- 2. factor
-- =============================================
CREATE TABLE public.factor (
    factor_id  INTEGER PRIMARY KEY,
    unit       VARCHAR(50),
    stressor   TEXT,
    extension  VARCHAR(100)
);

-- =============================================
-- 3. trade
-- =============================================
CREATE TABLE public.trade (
    trade_id    INTEGER      NOT NULL,
    year        SMALLINT     NOT NULL,
    region1     VARCHAR(10)  NOT NULL,
    region2     VARCHAR(10)  NOT NULL,
    industry1   VARCHAR(10)  NOT NULL REFERENCES public.industry(industry_id),
    industry2   VARCHAR(10)  NOT NULL REFERENCES public.industry(industry_id),
    amount      NUMERIC      NOT NULL,
    flow_type   VARCHAR(10)  NOT NULL,
    country     VARCHAR(10)  NOT NULL
);

CREATE INDEX idx_trade_year          ON public.trade(year);
CREATE INDEX idx_trade_country       ON public.trade(country);
CREATE INDEX idx_trade_flow_type     ON public.trade(flow_type);
CREATE INDEX idx_trade_year_country  ON public.trade(year, country, flow_type);

