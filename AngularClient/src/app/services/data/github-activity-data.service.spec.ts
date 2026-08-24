import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { GitHubActivityDataService, selectRepos } from './github-activity-data.service';
import { GitHubActivitySettings, GitHubRepo } from '../../interfaces/github-activity.interface';
import { environment } from '../../../environments/environment';

describe('GitHubActivityDataService', () => {
  let service: GitHubActivityDataService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(GitHubActivityDataService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('emits null when settings are disabled', done => {
    service.getActivity().subscribe(result => {
      expect(result).toBeNull();
      done();
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/github-activity-settings/public`);
    req.flush({ enabled: false, gitHubUsername: null, repoCount: 5, pinnedRepoNames: [] } as GitHubActivitySettings);
  });

  it('emits null when the settings fetch fails (404 - unconfigured)', done => {
    service.getActivity().subscribe(result => {
      expect(result).toBeNull();
      done();
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/github-activity-settings/public`);
    req.flush('not found', { status: 404, statusText: 'Not Found' });
  });

  it('fetches repos from GitHub and applies selection when enabled', done => {
    const repos: GitHubRepo[] = [
      { name: 'a', html_url: 'https://github.com/x/a', description: null, language: null, stargazers_count: 0, fork: false, pushed_at: '2026-01-01T00:00:00Z' }
    ];
    spyOn(window, 'fetch').and.resolveTo(new Response(JSON.stringify(repos), { status: 200 }));

    service.getActivity().subscribe(result => {
      expect(result).toEqual(repos);
      done();
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/github-activity-settings/public`);
    req.flush({ enabled: true, gitHubUsername: 'x', repoCount: 5, pinnedRepoNames: [] } as GitHubActivitySettings);
  });

  it('emits null when the GitHub fetch fails', done => {
    spyOn(window, 'fetch').and.resolveTo(new Response('rate limited', { status: 403 }));

    service.getActivity().subscribe(result => {
      expect(result).toBeNull();
      done();
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/github-activity-settings/public`);
    req.flush({ enabled: true, gitHubUsername: 'x', repoCount: 5, pinnedRepoNames: [] } as GitHubActivitySettings);
  });
});

describe('selectRepos', () => {
  const repos: GitHubRepo[] = [
    { name: 'pinned-repo', html_url: 'https://github.com/x/pinned-repo', description: null, language: null, stargazers_count: 0, fork: false, pushed_at: '2026-01-01T00:00:00Z' },
    { name: 'newest-repo', html_url: 'https://github.com/x/newest-repo', description: null, language: null, stargazers_count: 0, fork: false, pushed_at: '2026-08-01T00:00:00Z' },
    { name: 'older-repo', html_url: 'https://github.com/x/older-repo', description: null, language: null, stargazers_count: 0, fork: false, pushed_at: '2026-03-01T00:00:00Z' },
    { name: 'a-fork', html_url: 'https://github.com/x/a-fork', description: null, language: null, stargazers_count: 0, fork: true, pushed_at: '2026-09-01T00:00:00Z' }
  ];

  it('puts pinned repos first, then fills by most-recently-pushed, excluding forks', () => {
    const result = selectRepos(repos, ['pinned-repo'], 3);

    expect(result.map(r => r.name)).toEqual(['pinned-repo', 'newest-repo', 'older-repo']);
  });

  it('ignores pinned names that do not match any fetched repo', () => {
    const result = selectRepos(repos, ['does-not-exist'], 2);

    expect(result.map(r => r.name)).toEqual(['newest-repo', 'older-repo']);
  });

  it('counts pinned repos toward the total instead of adding to it', () => {
    const result = selectRepos(repos, ['pinned-repo'], 1);

    expect(result.map(r => r.name)).toEqual(['pinned-repo']);
  });

  it('includes a pinned fork, since pinning is a deliberate choice, but still excludes forks from automatic fill', () => {
    const result = selectRepos(repos, ['a-fork'], 4);

    expect(result.map(r => r.name)).toEqual(['a-fork', 'newest-repo', 'older-repo', 'pinned-repo']);
  });
});
