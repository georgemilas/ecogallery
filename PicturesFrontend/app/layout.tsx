export const metadata = {
  title: 'Pictures',
  description: 'GM Pictures Gallery',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <head>
        <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet"/>
      </head>
      <body>
        {children}
      </body>
    </html>
  );
}
