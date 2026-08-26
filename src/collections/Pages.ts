import type { CollectionConfig } from 'payload'

export const Pages: CollectionConfig = {
  slug: 'pages',
  labels: {
    singular: 'صفحه',
    plural: 'صفحات',
  },
  admin: {
    useAsTitle: 'title',
    defaultColumns: ['title', 'slug', 'status', 'updatedAt'],
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
      name: 'content',
      type: 'richText',
      label: 'محتوا',
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
