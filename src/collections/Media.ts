import type { CollectionConfig } from 'payload'

export const Media: CollectionConfig = {
  slug: 'media',
  labels: {
    singular: 'رسانه',
    plural: 'رسانه‌ها',
  },
  access: {
    read: () => true,
  },
  fields: [
    {
      name: 'alt',
      type: 'text',
      required: true,
      label: 'متن جایگزین (alt)',
    },
    {
      name: 'legacyAttachmentId',
      type: 'number',
      label: 'Legacy WP Attachment ID',
      index: true,
      admin: { position: 'sidebar', readOnly: true },
    },
    {
      name: 'legacyPath',
      type: 'text',
      label: 'Legacy uploads path',
      admin: { position: 'sidebar', readOnly: true },
    },
  ],
  upload: true,
}
