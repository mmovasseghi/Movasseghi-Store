import type { CollectionConfig } from 'payload'

export const Products: CollectionConfig = {
  slug: 'products',
  labels: {
    singular: 'محصول',
    plural: 'محصولات',
  },
  admin: {
    useAsTitle: 'name',
    defaultColumns: ['name', 'sku', 'regularPrice', 'stockQuantity', 'status', 'updatedAt'],
  },
  access: {
    read: ({ req }) => {
      if (req.user) return true
      return { status: { equals: 'published' } }
    },
  },
  fields: [
    {
      name: 'name',
      type: 'text',
      label: 'نام محصول',
      required: true,
    },
    {
      name: 'slug',
      type: 'text',
      label: 'اسلاگ',
      required: true,
      unique: true,
      index: true,
    },
    {
      name: 'status',
      type: 'select',
      label: 'وضعیت',
      required: true,
      defaultValue: 'draft',
      options: [
        { label: 'پیش‌نویس', value: 'draft' },
        { label: 'منتشر شده', value: 'published' },
        { label: 'آرشیو', value: 'archived' },
      ],
    },
    {
      name: 'sku',
      type: 'text',
      label: 'کد محصول (SKU)',
      index: true,
    },
    {
      type: 'row',
      fields: [
        {
          name: 'regularPrice',
          type: 'number',
          label: 'قیمت (تومان)',
          required: true,
          min: 0,
        },
        {
          name: 'salePrice',
          type: 'number',
          label: 'قیمت فروش ویژه',
          min: 0,
        },
      ],
    },
    {
      type: 'row',
      fields: [
        {
          name: 'stockQuantity',
          type: 'number',
          label: 'موجودی',
          defaultValue: 0,
        },
        {
          name: 'manageStock',
          type: 'checkbox',
          label: 'مدیریت موجودی',
          defaultValue: true,
        },
      ],
    },
    {
      name: 'shortDescription',
      type: 'textarea',
      label: 'توضیح کوتاه',
    },
    {
      name: 'description',
      type: 'richText',
      label: 'توضیحات کامل',
    },
    {
      name: 'legacyDescriptionHtml',
      type: 'textarea',
      label: 'توضیحات Legacy (HTML)',
      admin: {
        description: 'محتوای HTML اصلی WordPress — حفظ SEO و متن تجاری',
      },
    },
    {
      name: 'featuredImage',
      type: 'upload',
      relationTo: 'media',
      label: 'تصویر شاخص',
    },
    {
      name: 'gallery',
      type: 'array',
      label: 'گالری',
      fields: [
        {
          name: 'image',
          type: 'upload',
          relationTo: 'media',
          required: true,
        },
      ],
    },
    {
      name: 'categories',
      type: 'relationship',
      relationTo: 'categories',
      hasMany: true,
      label: 'دسته‌بندی‌ها',
    },
    {
      type: 'group',
      name: 'attributes',
      label: 'مشخصات',
      fields: [
        { name: 'material', type: 'text', label: 'جنس (مثلاً آملون)' },
        { name: 'capacityMl', type: 'number', label: 'ظرفیت (ml)' },
        { name: 'packSize', type: 'text', label: 'بسته‌بندی' },
        { name: 'heatResistanceC', type: 'number', label: 'مقاومت حرارتی (°C)' },
      ],
    },
    {
      type: 'group',
      name: 'b2b',
      label: 'عمده‌فروشی',
      fields: [
        { name: 'wholesalePrice', type: 'number', label: 'قیمت عمده' },
        { name: 'moq', type: 'number', label: 'حداقل سفارش (MOQ)' },
        { name: 'b2bOnly', type: 'checkbox', label: 'فقط B2B', defaultValue: false },
      ],
    },
    {
      type: 'group',
      name: 'seo',
      label: 'SEO',
      fields: [
        { name: 'title', type: 'text', label: 'عنوان SEO' },
        { name: 'description', type: 'textarea', label: 'توضیحات متا' },
        { name: 'focusKeyword', type: 'text', label: 'کلمه کلیدی' },
      ],
    },
    {
      name: 'legacyId',
      type: 'number',
      label: 'Legacy WP ID',
      admin: { position: 'sidebar', readOnly: true },
      index: true,
    },
  ],
}
