# 07 — Categories

**Product categories:** 22  
**Confidence:** CONFIRMED

Full table: [generated/product-categories.md](./generated/product-categories.md)

## Top-level commercial hubs

| Category | Products | SEO role |
|---|---:|---|
| **ظروف یکبار مصرف آملون** | 84 | **Primary commercial hub** — maps to "ظروف یکبار مصرف گیاهی" intent |
| پرفروش-ها | 11 | Merchandising / bestsellers |
| ظروف یکبارمصرف pp-ps | 0 | Empty — legacy plastic line placeholder |

## Category tree (simplified)

```
ظروف یکبار مصرف آملون (84)
├── ظروف یکبار مصرف فله آملون (55)     ← bulk / wholesale packaging
│   ├── کاســه ها ( آملون ) (10)
│   ├── ظروف چند خانه ( آملون ) (9)
│   ├── لیوان و فنجان ( آملون ) (7)
│   ├── دیس و بشقاب ( آملون ) (7)
│   ├── ظروف بسته بندی ( آملون ) (7)
│   ├── ظروف درب دار ( آملون ) (5)
│   ├── قاشق،چنگال،کارد ( آملون ) (5)
│   ├── درب ها ( آملون ) (5)
│   └── سـطل ها ( آملون ) (0)
└── ظروف یکبار مصرف شیرینک آملون (29)  ← shrink-wrapped retail packs
    ├── لیوان - فنجان ( آملون ) (5)
    ├── کاسه - سطل ( آملون ) (4)
    ├── بشقاب ( آملون ) (4)
    ├── قاشق،چنگال ( آملون ) (4)
    ├── دیس ( آملون ) (3)
    ├── ظروف غذا ( آملون ) (3)
    ├── بسته بندی و تک پرسی ( آملون ) (3)
    └── کاسه صدفی (3)
```

## Category descriptions (SEO assets)

Parent category **ظروف یکبار مصرف آملون** includes long Persian description:

> ظروف یکبار مصرف آملون ، کاملا گیاهی و برپایه نشاسته ذرت ساخته شده است.  
> ظروف یکبار مصرف آملون قابل تجزیه شدن و محیط زیست سازگار است...

Similar template for **فله** and **شیرینک** sub-branches.

**Migration:** Preserve descriptions initially; mark environmental decomposition claims as **SOURCE_REQUIRED** for legal/compliance review.

## URL pattern

```
https://www.ayrik-cornstarch.com/product-category/{slug}/
```

## Woodmart category meta (termmeta)

Fields present but often empty in backup:

- `title_image`, `category_icon`, `category_icon_alt`
- `category_extra_description_type` (text/html block)
- `display_type`, `thumbnail_id`, `order`

## Blog categories

Standard WordPress `category` taxonomy — minimal use compared to product catalog.

## Migration mapping to new IA

| Legacy hub | New platform intent |
|---|---|
| ظروف یکبار مصرف آملون | Primary category landing for commercial SEO targets |
| فله vs شیرینک split | Preserve B2B (bulk) vs retail packaging UX |
| پرفروش-ها | Featured/bestseller collection — not a SEO doorway page |

## Target query alignment

| Target query | Recommended legacy anchor |
|---|---|
| ظروف یکبار مصرف گیاهی | Category hub + shop — **NOT** blog posts |
| ظروف یکبار مصرف | Shop + parent category |
| ظرف یکبار مصرف گیاهی | Product pages + supporting category copy |

Do **not** create near-duplicate landing pages for wording variants.
