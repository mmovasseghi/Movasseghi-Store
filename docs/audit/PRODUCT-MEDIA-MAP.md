# PRODUCT-MEDIA-MAP — Legacy → New Platform

**Status:** CONFIRMED (DB relationships) / PENDING (file copy + Payload upload)
**Products:** 95 published | **With primary image:** 82

## Mapping schema

```
Legacy Product (WP ID)
  → Primary Image (attachment ID → _wp_attached_file → URL)
  → Gallery (CSV attachment IDs)
  → New Product (Payload slug / legacyId)
  → New Media (Payload media collection)
```

## Per-product map (summary table)

| Legacy ID | Product | SKU | Primary | Gallery # | File on disk | Status |
|---:|---|---|---:|---:|---|---|
| 268 | لیوان 500 سی سی آملون | — | 269 | 1 | ✅ | PENDING |
| 272 | لیوان 350 سی سی آملون | — | 281 | 1 | ✅ | PENDING |
| 273 | لیوان 250 سی سی آملون | — | 280 | 1 | ✅ | PENDING |
| 274 | لیوان 200 سی سی آملون | — | 279 | 1 | ✅ | PENDING |
| 275 | لیوان 170 سی سی آملون | — | 278 | 1 | ✅ | PENDING |
| 284 | پایه فنجان آملون | — | 285 | 1 | ✅ | PENDING |
| 289 | فنجان آملون | — | 285 | 1 | ✅ | PENDING |
| 290 | کاسه صدفی 1000 سی سی آملون | — | 292 | 1 | ✅ | PENDING |
| 294 | کاسه صدفی 500 سی سی آملون | — | 295 | 1 | ✅ | PENDING |
| 296 | کاسه صدفی 300 سی سی آملون | — | 300 | 1 | ✅ | PENDING |
| 298 | کاسه 550 سی سی آملون | — | 297 | 1 | ✅ | PENDING |
| 301 | کاسه 280 سی سی آملون | — | 302 | 1 | ✅ | PENDING |
| 303 | کاسه 400 سی سی آملون | — | 302 | 1 | ✅ | PENDING |
| 304 | کاسه 1300 سی سی آملون | — | 305 | 1 | ✅ | PENDING |
| 307 | کاسه 200 سی سی آملون | — | 308 | 1 | ✅ | PENDING |
| 309 | کاسه چهارگوش آملون | — | 310 | 1 | ✅ | PENDING |
| 312 | سیب زمینی خوری آملون | — | — | 1 | — | NO_PRIMARY_IMAGE |
| 318 | کاسه صدفی 500 سی سی آملون | — | 319 | 0 | ✅ | PENDING |
| 321 | کاسه صدفی 1000 سی سی آملون | — | 322 | 0 | ✅ | PENDING |
| 323 | کاسه صدفی 300 سی سی آملون | — | 324 | 0 | ✅ | PENDING |
| 325 | بشقاب بزرگ آملون | — | 326 | 0 | ✅ | PENDING |
| 327 | بشقاب آملون | — | 328 | 0 | ✅ | PENDING |
| 331 | پیش دستی آملون | — | 332 | 0 | ✅ | PENDING |
| 333 | کیک خوری آملون | — | 334 | 0 | ✅ | PENDING |
| 335 | دیس کوچک آملون | — | 336 | 0 | ✅ | PENDING |
| 337 | دیس متوسط آملون | — | 338 | 0 | ✅ | PENDING |
| 339 | دیس بزرگ آملون | — | 340 | 0 | ✅ | PENDING |
| 341 | ظرف غذا طرح آلومینیوم آملون | — | 342 | 0 | ✅ | PENDING |
| 343 | ظرف غذا تک خانه آملون | — | 344 | 0 | ✅ | PENDING |
| 345 | ظرف غذا دوخانه آملون | — | 346 | 0 | ✅ | PENDING |
| 347 | ظرف تک پرسی آملون | — | 348 | 0 | ✅ | PENDING |
| 349 | ظرف سه خانه بسته بندی آملون | — | 350 | 0 | ✅ | PENDING |
| 351 | ظرف دوخانه بسته بندی آملون | — | 352 | 0 | ✅ | PENDING |
| 353 | قاشق چنگال آملون | — | 354 | 0 | ✅ | PENDING |
| 355 | چنگال آملون | — | 356 | 0 | ✅ | PENDING |
| 357 | کارد 12 تایی آملون | — | 358 | 0 | ✅ | PENDING |
| 359 | قاشق 12 عددی آملون | — | 360 | 0 | ✅ | PENDING |
| 361 | دیس کوچک آملون | — | 364 | 0 | ✅ | PENDING |
| 362 | دیس متوسط آملون | — | 365 | 0 | ✅ | PENDING |
| 363 | دیس بزرگ آملون | — | 366 | 0 | ✅ | PENDING |
| 367 | بشقاب آملون | — | 371 | 0 | ✅ | PENDING |
| 368 | بشقاب ساده آملون | — | 372 | 0 | ✅ | PENDING |
| 369 | پیش دستی آملون | — | 373 | 0 | ✅ | PENDING |
| 370 | بشقاب بزرگ آملون | — | 374 | 0 | ✅ | PENDING |
| 375 | ظرف غذا تک خانه آملون | — | 388 | 0 | ✅ | PENDING |
| 376 | ظرف سه خانه کبابی آملون | — | 389 | 0 | ✅ | PENDING |
| 377 | ظرف چهارخونه فانتزی آملون | — | 396 | 0 | ✅ | PENDING |
| 378 | ظرف غذا شش خانه فانتزی آملون | — | 391 | 0 | ✅ | PENDING |
| 379 | دوخانه کبابی آملون | — | 392 | 0 | ✅ | PENDING |
| 380 | دو خانه مرغی آملون | — | 393 | 0 | ✅ | PENDING |
| 382 | چهارخانه کبابی آملون | — | 394 | 0 | ✅ | PENDING |
| 383 | ظرف غذا دوخانه آملون | — | 387 | 0 | ✅ | PENDING |
| 386 | چهارخانه مرغی آملون | — | 395 | 0 | ✅ | PENDING |
| 399 | درب 195 آملون | — | 404 | 0 | ✅ | PENDING |
| 400 | درب 175 آملون | — | 405 | 0 | ✅ | PENDING |
| 401 | درب 150 آملون | — | — | 0 | — | NO_PRIMARY_IMAGE |
| 402 | درب 125 آملون | — | 407 | 0 | ✅ | PENDING |
| 403 | درب 105 آملون | — | 408 | 0 | ✅ | PENDING |
| 409 | قاشق آملون | — | 414 | 0 | ✅ | PENDING |
| 410 | کارد آملون | — | 415 | 0 | ✅ | PENDING |
| 411 | قاشق چای خوری آملون | — | 416 | 0 | ✅ | PENDING |
| 412 | نی آملون | — | 417 | 0 | ✅ | PENDING |
| 413 | چنگال آملون | — | 418 | 0 | ✅ | PENDING |
| 419 | ظرف همبرگر خوری آملون | — | 426 | 0 | ✅ | PENDING |
| 420 | ظرف سه خانه درب دار آملون | — | 427 | 0 | ✅ | PENDING |
| 421 | ظرف تک پرسی 1000 آملون | — | 428 | 0 | ✅ | PENDING |
| 423 | ظرف غذا دوپرسی آملون | — | 430 | 0 | ✅ | PENDING |
| 425 | دوخانه درب دار آملون | — | 431 | 0 | ✅ | PENDING |
| 432 | ظرف کبابی طرح آلومینیوم آملون | — | 440 | 0 | ✅ | PENDING |
| 433 | ظرف غذا طرح آلومینیوم 800 آملون | — | 441 | 0 | ✅ | PENDING |
| 434 | ظرف غذا طرح آلومینیوم 1200 آملون | — | 442 | 0 | ✅ | PENDING |
| 435 | ظرف بسته بندی کم عمق آملون | — | 443 | 0 | ✅ | PENDING |
| 437 | ظرف بسته بندی عمیق آملون | — | 444 | 0 | ✅ | PENDING |
| 438 | ظرف بسته بندی سه خانه آملون | — | 445 | 0 | ✅ | PENDING |
| 439 | ظرف بسته بندی دوخانه آملون | — | 446 | 0 | ✅ | PENDING |
| 447 | لیوان 200 شیرینگ 24 تایی آملون | — | 452 | 0 | ✅ | PENDING |
| 448 | فنجان با نگهدارنده آملون | — | 454 | 0 | ✅ | PENDING |
| 449 | فنجان 24 تایی آملون | — | 455 | 0 | ✅ | PENDING |
| 450 | لیوان 200 (12 تایی) آملون | — | 456 | 0 | ✅ | PENDING |
| 451 | لیوان 250 (12 تایی) آملون | — | 457 | 0 | ✅ | PENDING |
| 458 | کاسه چهارگوش آملون | — | 462 | 0 | ✅ | PENDING |
| 459 | سطل 1000 بلند آملون | — | 463 | 0 | ✅ | PENDING |
| 460 | کاسه 300 آملون | — | 464 | 0 | ✅ | PENDING |
| 461 | کاسه 550 آملون | — | 465 | 0 | ✅ | PENDING |
| 743 | ظرف خورشتی پ پ الیکاس | — | — | 0 | — | NO_PRIMARY_IMAGE |
| 787 | سطل 1200 شفاف پ پ | — | — | 0 | — | NO_PRIMARY_IMAGE |
| 789 | درب سطل1000 و 1200 | — | — | 0 | — | NO_PRIMARY_IMAGE |
| 790 | سطل1300 شفاف پ پ | — | — | 0 | — | NO_PRIMARY_IMAGE |
| 791 | سطل 1500 شفاف پ پ | — | — | 0 | — | NO_PRIMARY_IMAGE |
| 792 | سطل 1700 شفاف پ پ الیکاس | — | — | 0 | — | NO_PRIMARY_IMAGE |
| 794 | درب سطل 1300/1500/1700 پ پ | — | — | 0 | — | NO_PRIMARY_IMAGE |
| 795 | سطل 1800 شفاف پ پ الیکاس | — | — | 0 | — | NO_PRIMARY_IMAGE |
| 796 | سطل 2000 شفاف پ پ | — | — | 0 | — | NO_PRIMARY_IMAGE |
| 797 | سطل 2500 پ پ | — | — | 0 | — | NO_PRIMARY_IMAGE |
| 798 | سطل 3000 پ پ | — | — | 0 | — | NO_PRIMARY_IMAGE |

## Detailed example (first 5 products)

### لیوان 500 سی سی آملون (ID 268)

- **Legacy URL:** https://www.ayrik-cornstarch.com/product/%25d9%2584%25db%258c%25d9%2588%25d8%25a7%25d9%2586-500-%25d8%25b3%25db%258c-%25d8%25b3%25db%258c-%25d8%25a2%25d9%2585%25d9%2584%25d9%2588%25d9%2586/
- **SKU:** NOT FOUND
- **Primary image ID:** 269
- **Primary file:** `2024/01/لیوان-500.png`
- **Legacy CDN URL:** https://www.ayrik-cornstarch.com/wp-content/uploads/2024/01/لیوان-500.png
- **Alt text:** EMPTY — SOURCE_REQUIRED
- **Dimensions:** 600×600
- **Gallery IDs:** 270
- **Migration status:** PENDING

### لیوان 350 سی سی آملون (ID 272)

- **Legacy URL:** https://www.ayrik-cornstarch.com/product/%25d9%2584%25db%258c%25d9%2588%25d8%25a7%25d9%2586-350-%25d8%25b3%25db%258c-%25d8%25b3%25db%258c-%25d8%25a2%25d9%2585%25d9%2584%25d9%2588%25d9%2586/
- **SKU:** NOT FOUND
- **Primary image ID:** 281
- **Primary file:** `2024/01/لیوان-350.png`
- **Legacy CDN URL:** https://www.ayrik-cornstarch.com/wp-content/uploads/2024/01/لیوان-350.png
- **Alt text:** EMPTY — SOURCE_REQUIRED
- **Dimensions:** 600×600
- **Gallery IDs:** 270
- **Migration status:** PENDING

### لیوان 250 سی سی آملون (ID 273)

- **Legacy URL:** https://www.ayrik-cornstarch.com/product/%25d9%2584%25db%258c%25d9%2588%25d8%25a7%25d9%2586-250-%25d8%25b3%25db%258c-%25d8%25b3%25db%258c-%25d8%25a2%25d9%2585%25d9%2584%25d9%2588%25d9%2586/
- **SKU:** NOT FOUND
- **Primary image ID:** 280
- **Primary file:** `2024/01/لیوان-250.png`
- **Legacy CDN URL:** https://www.ayrik-cornstarch.com/wp-content/uploads/2024/01/لیوان-250.png
- **Alt text:** EMPTY — SOURCE_REQUIRED
- **Dimensions:** 500×500
- **Gallery IDs:** 270
- **Migration status:** PENDING

### لیوان 200 سی سی آملون (ID 274)

- **Legacy URL:** https://www.ayrik-cornstarch.com/product/%25d9%2584%25db%258c%25d9%2588%25d8%25a7%25d9%2586-200-%25d8%25b3%25db%258c-%25d8%25b3%25db%258c-%25d8%25a2%25d9%2585%25d9%2584%25d9%2588%25d9%2586/
- **SKU:** NOT FOUND
- **Primary image ID:** 279
- **Primary file:** `2024/01/لیوان-200.png`
- **Legacy CDN URL:** https://www.ayrik-cornstarch.com/wp-content/uploads/2024/01/لیوان-200.png
- **Alt text:** EMPTY — SOURCE_REQUIRED
- **Dimensions:** 500×500
- **Gallery IDs:** 270
- **Migration status:** PENDING

### لیوان 170 سی سی آملون (ID 275)

- **Legacy URL:** https://www.ayrik-cornstarch.com/product/%25d9%2584%25db%258c%25d9%2588%25d8%25a7%25d9%2586-170-%25d8%25b3%25db%258c-%25d8%25b3%25db%258c-%25d8%25a2%25d9%2585%25d9%2584%25d9%2588%25d9%2586/
- **SKU:** NOT FOUND
- **Primary image ID:** 278
- **Primary file:** `2024/01/لیوان-170.png`
- **Legacy CDN URL:** https://www.ayrik-cornstarch.com/wp-content/uploads/2024/01/لیوان-170.png
- **Alt text:** EMPTY — SOURCE_REQUIRED
- **Dimensions:** 500×500
- **Gallery IDs:** 270
- **Migration status:** PENDING

## Image SEO rules (migration)

- Alt text from `_wp_attachment_image_alt` when present
- Fallback: product name + image role (e.g. "بشقاب آملون — تصویر محصول")
- No keyword stuffing
- Preserve meaningful filenames where possible

## Full JSON

See `docs/audit/generated/product-media-map.json` for complete machine-readable map.
