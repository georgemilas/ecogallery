import { AuthProvider } from './contexts/AuthContext';

export const metadata = {
  title: 'Milas Gallery',
  description: 'GM Pictures Gallery',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <head>
        <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet"/>
      </head>
      <body style={{ backgroundColor: 'rgba(0, 0, 0, 1)', margin: 0, padding: 0, color: 'white' }}>
        <AuthProvider>
          {children}
        </AuthProvider>
      </body>
    </html>
  );
}
