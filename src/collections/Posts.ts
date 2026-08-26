import type { CollectionConfig } from 'payload'

export const Posts: CollectionConfig = {
  slug: 'posts',
  labels: {
    singular: 'مقاله',
    plural: 'مقالات',
  },
  admin: {
    useAsTitle: 'title',
    defaultColumns: ['title', 'slug', 'status', 'publishedAt', 'updatedAt'],
  },
  access: {
    read: ({ req }) => {
      if (req.user) return true
      return { status: { equals: 'published' } }
    },
  },
  fields: [
    {
      name: 'title',
      type: 'text',
      label: 'عنوان',
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
      name: 'legacyId',
      type: 'number',
      label: 'شناسه قدیمی',
      index: true,
      admin: { readOnly: true },
    },
    {
      name: 'excerpt',
      type: 'textarea',
      label: 'خلاصه',
    },
    {
      name: 'legacyContentHtml',
      type: 'code',
      label: 'محتوای HTML',
      admin: { language: 'html' },
    },
    {
      name: 'publishedAt',
      type: 'date',
      label: 'تاریخ انتشار',
      admin: { date: { pickerAppearance: 'dayAndTime' } },
    },
    {
      name: 'status',
      type: 'select',
      label: 'وضعیت',
      defaultValue: 'draft',
      options: [
        { label: 'پیش‌نویس', value: 'draft' },
        { label: 'منتشر شده', value: 'published' },
      ],
    },
    {
      type: 'group',
      name: 'seo',
      label: 'SEO',
      fields: [
        { name: 'title', type: 'text', label: 'عنوان SEO' },
        { name: 'description', type: 'textarea', label: 'توضیحات متا' },
      ],
    },
  ],
}
