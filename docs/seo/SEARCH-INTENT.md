# Search Intent Map

## Primary queries

### 1. ظروف یکبار مصرف گیاهی

| Attribute | Assessment |
|---|---|
| Intent | **Commercial** — buyer seeking plant-based disposables |
| SERP type (INFERRED) | Category pages, shops, wholesalers |
| Legacy match | Strong — آملون category family + product copy |
| Best page type | **Category landing + shop** (NOT blog) |
| Competitor examples | yekbaar.com category, armani724.com category, ecoclicky.com shop |

### 2. ظروف یکبار مصرف

| Attribute | Assessment |
|---|---|
| Intent | **Commercial** — broader disposable containers |
| Legacy match | Shop + all categories (includes plant-based focus) |
| Best page type | Shop archive + filtered category |
| Note | Legacy is specialized (plant-based), not general plastic |

### 3. ظرف یکبار مصرف گیاهی

| Attribute | Assessment |
|---|---|
| Intent | **Commercial/transactional** — singular product search |
| Legacy match | Individual product pages |
| Best page type | Product pages + category support |
| Cannibalization risk | Do not create separate landing competing with #1 |

## Supporting intents

| Query pattern | Intent | Destination |
|---|---|---|
| {size} لیوان آملون | Transactional | Product page |
| قیمت ظروف آملون | Commercial | Pricing page + products |
| خرید عمده ظروف گیاهی | B2B commercial | Bulk category + contact/quote |
| ظروف کترینگ | B2B commercial | Use-case section → bulk category |

## Intent routing rules (new platform)

```
Informational query → useful answer → relevant category/product → trust → convert
Commercial query → category/shop → filter → product → cart/contact
B2B query → bulk category → quote/phone/org invoice path
```

## Legacy mismatch

209 blog posts mostly **do not** serve Persian commercial intent — spam. Do not use post archive as SEO strategy template.
