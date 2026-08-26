import type { CollectionConfig } from 'payload'

export const Orders: CollectionConfig = {
  slug: 'orders',
  labels: {
    singular: 'سفارش',
    plural: 'سفارش‌ها',
  },
  admin: {
    useAsTitle: 'orderNumber',
    defaultColumns: ['orderNumber', 'customerName', 'customerPhone', 'subtotal', 'status', 'createdAt'],
  },
  access: {
    create: () => true,
    read: ({ req }) => Boolean(req.user),
    update: ({ req }) => Boolean(req.user),
    delete: ({ req }) => Boolean(req.user),
  },
  fields: [
    {
      name: 'orderNumber',
      type: 'text',
      label: 'شماره سفارش',
      required: true,
      unique: true,
      index: true,
    },
    {
      name: 'customerName',
      type: 'text',
      label: 'نام مشتری',
      required: true,
    },
    {
      name: 'customerPhone',
      type: 'text',
      label: 'موبایل',
      required: true,
    },
    {
      name: 'note',
      type: 'textarea',
      label: 'توضیحات',
    },
    {
      name: 'paymentMethod',
      type: 'select',
      label: 'روش پرداخت',
      required: true,
      options: [
        { label: 'تلفنی', value: 'phone' },
        { label: 'کارت به کارت', value: 'card_to_card' },
        { label: 'آنلاین', value: 'online' },
      ],
    },
    {
      name: 'shippingMethod',
      type: 'select',
      label: 'روش ارسال',
      required: true,
      options: [
        { label: 'ارسال فروشنده', value: 'seller' },
        { label: 'خودروی مشتری', value: 'customer' },
      ],
    },
    {
      name: 'items',
      type: 'json',
      label: 'اقلام',
      required: true,
    },
    {
      name: 'subtotal',
      type: 'number',
      label: 'جمع (تومان)',
      required: true,
      min: 0,
    },
    {
      name: 'status',
      type: 'select',
      label: 'وضعیت',
      defaultValue: 'pending',
      options: [
        { label: 'در انتظار', value: 'pending' },
        { label: 'تأیید شده', value: 'confirmed' },
        { label: 'لغو شده', value: 'cancelled' },
      ],
    },
  ],
}
