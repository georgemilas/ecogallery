export const metadata = {
  title: 'Weather Reports',
  description: 'Residio weather reports',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body style={{ fontFamily: 'sans-serif', margin: 0, padding: '1rem', background: '#f7f7f7' }}>
        <h1>Weather Reports</h1>
        {children}
      </body>
    </html>
  );
}
