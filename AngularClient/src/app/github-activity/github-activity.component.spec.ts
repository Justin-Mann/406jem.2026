import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { GitHubActivityComponent } from './github-activity.component';
import { GitHubActivityDataService } from '../services/data/github-activity-data.service';
import { GitHubRepo } from '../interfaces/github-activity.interface';

const repos: GitHubRepo[] = [
  { name: 'pinned-repo', html_url: 'https://github.com/jem/pinned-repo', description: 'Pinned one', language: 'C#', stargazers_count: 2, fork: false, pushed_at: '2026-01-01T00:00:00Z' },
  { name: 'newest-repo', html_url: 'https://github.com/jem/newest-repo', description: 'Newest', language: 'TypeScript', stargazers_count: 5, fork: false, pushed_at: '2026-08-01T00:00:00Z' }
];

describe('GitHubActivityComponent', () => {
  let fixture: ComponentFixture<GitHubActivityComponent>;
  let dataServiceSpy: jasmine.SpyObj<GitHubActivityDataService>;

  async function setup(): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [GitHubActivityComponent],
      providers: [{ provide: GitHubActivityDataService, useValue: dataServiceSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(GitHubActivityComponent);
  }

  beforeEach(() => {
    dataServiceSpy = jasmine.createSpyObj('GitHubActivityDataService', ['getActivity']);
  });

  it('renders nothing when the data service emits null (disabled/unconfigured)', async () => {
    dataServiceSpy.getActivity.and.returnValue(of(null));
    await setup();

    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).not.toContain('GitHub Activity');
  });

  it('renders nothing when the data service errors', async () => {
    dataServiceSpy.getActivity.and.returnValue(throwError(() => new Error('boom')));
    await setup();

    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).not.toContain('GitHub Activity');
  });

  it('renders each repo with name, description, and language, but not star count', async () => {
    dataServiceSpy.getActivity.and.returnValue(of(repos));
    await setup();

    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('GitHub Activity');
    expect(compiled.textContent).toContain('pinned-repo');
    expect(compiled.textContent).toContain('Pinned one');
    expect(compiled.textContent).toContain('C#');
    expect(compiled.querySelector('.github-activity-stars')).toBeNull();

    const link = compiled.querySelector('a.github-activity-name') as HTMLAnchorElement;
    expect(link.getAttribute('href')).toBe('https://github.com/jem/pinned-repo');
    expect(link.getAttribute('target')).toBe('_blank');
  });

  it('renders language when a repo has no description', async () => {
    const noDescriptionRepo: GitHubRepo[] = [
      { name: 'no-desc-repo', html_url: 'https://github.com/jem/no-desc-repo', description: null, language: 'C#', stargazers_count: 0, fork: false, pushed_at: '2026-01-01T00:00:00Z' }
    ];
    dataServiceSpy.getActivity.and.returnValue(of(noDescriptionRepo));
    await setup();

    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.github-activity-description')).toBeNull();
    expect(compiled.querySelector('.github-activity-meta')).not.toBeNull();
    expect(compiled.textContent).toContain('C#');
  });
});
