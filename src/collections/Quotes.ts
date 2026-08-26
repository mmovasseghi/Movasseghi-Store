import type { CollectionConfig } from 'payload'

export const Quotes: CollectionConfig = {
  slug: 'quotes',
  labels: {
    singular: 'درخواست عمده',
    plural: 'درخواست‌های عمده',
  },
  admin: {
    useAsTitle: 'quoteNumber',
    defaultColumns: ['quoteNumber', 'companyName', 'contactPhone', 'status', 'createdAt'],
  },
  access: {
    create: () => true,
    read: ({ req }) => Boolean(req.user),
    update: ({ req }) => Boolean(req.user),
    delete: ({ req }) => Boolean(req.user),
  },
  fields: [
    {
      name: 'quoteNumber',
      type: 'text',
      label: 'شماره درخواست',
      required: true,
      unique: true,
      index: true,
    },
    {
      name: 'companyName',
      type: 'text',
      label: 'نام کسب‌وکار',
      required: true,
    },
    {
      name: 'contactName',
      type: 'text',
      label: 'نام تماس',
      required: true,
    },
    {
      name: 'contactPhone',
      type: 'text',
      label: 'موبایل',
      required: true,
    },
    {
      name: 'city',
      type: 'text',
      label: 'شهر',
    },
    {
      name: 'productsNote',
      type: 'textarea',
      label: 'محصولات و تعداد',
      required: true,
    },
    {
      name: 'shippingPreference',
      type: 'select',
      label: 'روش تحویل',
      options: [
        { label: 'ارسال فروشگاه', value: 'seller' },
        { label: 'خودروی مشتری', value: 'customer' },
        { label: 'نامشخص', value: 'unknown' },
      ],
      defaultValue: 'unknown',
    },
    {
      name: 'note',
      type: 'textarea',
      label: 'توضیحات',
    },
    {
      name: 'status',
      type: 'select',
      label: 'وضعیت',
      defaultValue: 'new',
      options: [
        { label: 'جدید', value: 'new' },
        { label: 'در پیگیری', value: 'in_progress' },
        { label: 'پاسخ داده شده', value: 'answered' },
        { label: 'بسته شده', value: 'closed' },
      ],
    },
  ],
}
