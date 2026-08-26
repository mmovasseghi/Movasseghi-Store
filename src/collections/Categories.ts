import type { CollectionConfig } from 'payload'

export const Categories: CollectionConfig = {
  slug: 'categories',
  labels: {
    singular: 'دسته‌بندی',
    plural: 'دسته‌بندی‌ها',
  },
  admin: {
    useAsTitle: 'name',
    defaultColumns: ['name', 'slug', 'parent', 'productCount', 'updatedAt'],
  },
  access: {
    read: () => true,
  },
  fields: [
    {
      name: 'name',
      type: 'text',
      label: 'نام',
      required: true,
    },
    {
      name: 'slug',
      type: 'text',
      label: 'اسلاگ',
      required: true,
      unique: true,
      index: true,
      admin: {
        description: 'URL-friendly identifier, e.g. ظروف-یکبار-مصرف-آملون',
      },
    },
    {
      name: 'description',
      type: 'textarea',
      label: 'توضیحات',
    },
    {
      name: 'parent',
      type: 'relationship',
      relationTo: 'categories',
      label: 'دسته والد',
    },
    {
      name: 'sortOrder',
      type: 'number',
      label: 'ترتیب',
      defaultValue: 0,
    },
    {
      name: 'productCount',
      type: 'number',
      label: 'تعداد محصول (cache)',
      admin: {
        readOnly: true,
      },
    },
    {
      name: 'image',
      type: 'upload',
      relationTo: 'media',
      label: 'تصویر',
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
  ],
}
